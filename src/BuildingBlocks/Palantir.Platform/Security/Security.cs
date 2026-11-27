using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Palantir.Constants;
using Palantir.Platform.Caching;
using Palantir.Platform.Security.Permissions;

namespace Palantir.Platform.Security;

public record PermissionToken(
    Guid UserId,
    string Username,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset ExpiresAtUtc
);

public interface ITokenExchangeService
{
    Task<string> ExchangeTokenAsync(string incomingUserToken, string targetAudience, CancellationToken ct = default);
}

public class KeycloakTokenExchangeService : ITokenExchangeService
{
    private readonly HttpClient _httpClient;
    private readonly IKeyValueStore _cache;
    private readonly ILogger<KeycloakTokenExchangeService> _logger;

    public KeycloakTokenExchangeService(
        HttpClient httpClient,
        IKeyValueStore cache,
        ILogger<KeycloakTokenExchangeService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
    }

    private sealed class TokenExchangeResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }

    public async Task<string> ExchangeTokenAsync(string incomingUserToken, string targetAudience, CancellationToken ct = default)
    {
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(incomingUserToken)));
        var cacheKey = MessagingConstants.CacheKeys.TokenExchange(targetAudience, tokenHash);

        var cached = await _cache.GetAsync(cacheKey, ct);
        if (!string.IsNullOrEmpty(cached))
        {
            return cached;
        }

        var parameters = new Dictionary<string, string>
        {
            ["grant_type"] = "urn:ietf:params:oauth:grant-type:token-exchange",
            ["subject_token"] = incomingUserToken,
            ["subject_token_type"] = "urn:ietf:params:oauth:token-type:access_token",
            ["audience"] = targetAudience
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/realms/palantir/protocol/openid-connect/token")
        {
            Content = new FormUrlEncodedContent(parameters)
        };

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenExchangeResponse>(cancellationToken: ct);
        if (tokenResponse == null || string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
        {
            throw new InvalidOperationException($"Token exchange failed for target audience '{targetAudience}'.");
        }

        var ttl = TimeSpan.FromSeconds(Math.Max(30, tokenResponse.ExpiresIn * 0.8));
        await _cache.SetAsync(cacheKey, tokenResponse.AccessToken, ttl, ct);
        _logger.LogDebug("Exchanged and cached token for audience {Audience}.", targetAudience);

        return tokenResponse.AccessToken;
    }
}

public sealed class TokenExchangeDelegatingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ITokenExchangeService _tokenExchangeService;
    private readonly string _targetAudience;

    public TokenExchangeDelegatingHandler(
        IHttpContextAccessor httpContextAccessor,
        ITokenExchangeService tokenExchangeService,
        string targetAudience)
    {
        _httpContextAccessor = httpContextAccessor;
        _tokenExchangeService = tokenExchangeService;
        _targetAudience = targetAudience;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var authHeader = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var incomingToken = authHeader["Bearer ".Length..].Trim();
            if (!string.IsNullOrEmpty(incomingToken))
            {
                try
                {
                    var exchanged = await _tokenExchangeService.ExchangeTokenAsync(incomingToken, _targetAudience, cancellationToken);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", exchanged);
                }
                catch
                {
                    // Fallback to forwarding the validated subject token if Keycloak token exchange is not yet provisioned
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", incomingToken);
                }
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}

public interface IPermissionTokenManager
{
    Task<PermissionToken?> GetPermissionTokenAsync(Guid userId, CancellationToken ct = default);
    Task SetPermissionTokenAsync(PermissionToken token, TimeSpan ttl, CancellationToken ct = default);
    Task InvalidatePermissionTokenAsync(Guid userId, CancellationToken ct = default);
    Task<bool> HasPermissionAsync(Guid userId, string requiredPermission, CancellationToken ct = default);
}

public class PostgresPermissionTokenManager : IPermissionTokenManager
{
    private readonly IKeyValueStore _store;
    private readonly ILogger<PostgresPermissionTokenManager> _logger;

    public PostgresPermissionTokenManager(IKeyValueStore store, ILogger<PostgresPermissionTokenManager> logger)
    {
        _store = store;
        _logger = logger;
    }

    public async Task<PermissionToken?> GetPermissionTokenAsync(Guid userId, CancellationToken ct = default)
    {
        var key = MessagingConstants.CacheKeys.PermissionToken(userId);
        var json = await _store.GetAsync(key, ct);
        if (string.IsNullOrEmpty(json)) return null;

        try
        {
            return JsonSerializer.Deserialize<PermissionToken>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize permission token for user {UserId}", userId);
            return null;
        }
    }

    public Task SetPermissionTokenAsync(PermissionToken token, TimeSpan ttl, CancellationToken ct = default)
    {
        var key = MessagingConstants.CacheKeys.PermissionToken(token.UserId);
        var json = JsonSerializer.Serialize(token);
        return _store.SetAsync(key, json, ttl, ct);
    }

    public Task InvalidatePermissionTokenAsync(Guid userId, CancellationToken ct = default)
    {
        var key = MessagingConstants.CacheKeys.PermissionToken(userId);
        return _store.RemoveAsync(key, ct);
    }

    public async Task<bool> HasPermissionAsync(Guid userId, string requiredPermission, CancellationToken ct = default)
    {
        var token = await GetPermissionTokenAsync(userId, ct);
        if (token == null) return false;
        return token.Permissions.Contains(requiredPermission, StringComparer.OrdinalIgnoreCase);
    }
}

public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }
    public PermissionRequirement(string permission) => Permission = permission;
}

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IPermissionEvaluator _evaluator;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PermissionAuthorizationHandler(
        IPermissionEvaluator evaluator,
        IHttpContextAccessor httpContextAccessor)
    {
        _evaluator = evaluator;
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? context.User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return;
        }

        if (context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return;
        }

        var (dimension, action, resourceType) = PermissionEvaluator.ParsePermissionString(requirement.Permission);
        var permContext = PermissionContext.FromHttpContext(_httpContextAccessor.HttpContext, userIdClaim);
        var resource = ResourceTemplate.Any(resourceType ?? dimension.Name);

        var result = await _evaluator.EvaluateAsync(userId, dimension, action, resource, permContext);
        if (result.IsAllowed)
        {
            context.Succeed(requirement);
        }
    }
}

public static class SecurityExtensions
{
    public static IServiceCollection AddPalantirSecurity(this IServiceCollection services, string? keycloakBaseUrl = null)
    {
        services.AddHttpContextAccessor();
        services.TryAddSingleton<IPermissionTokenManager, PostgresPermissionTokenManager>();
        services.TryAddSingleton<IPermissionEvaluator, PermissionEvaluator>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPermissionSpecification, DimensionActionSpec>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPermissionSpecification, ResourceMatchSpec>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPermissionSpecification, OwnershipSpec>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPermissionSpecification, TradingHoursSpec>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPermissionSpecification, NetworkConditionSpec>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAuthorizationHandler, PermissionAuthorizationHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAuthorizationHandler, DimensionAuthorizationHandler>());

        if (!string.IsNullOrWhiteSpace(keycloakBaseUrl))
        {
            services.AddHttpClient<ITokenExchangeService, KeycloakTokenExchangeService>(client =>
            {
                client.BaseAddress = new Uri(keycloakBaseUrl);
            });
        }
        else
        {
            services.AddHttpClient<ITokenExchangeService, KeycloakTokenExchangeService>();
        }

        services.AddAuthorization(options =>
        {
            foreach (var permission in Palantir.Constants.Permissions.All)
            {
                options.AddPolicy(permission, policy => policy.Requirements.Add(new PermissionRequirement(permission)));
            }
        });

        return services;
    }

    public static IServiceCollection AddPalantirJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        string realm = "palantir")
    {
        var keycloakUrl = configuration["Keycloak:Url"]
                          ?? configuration["services:keycloak:http:0"]
                          ?? "http://localhost:8080";

        return services.AddPalantirJwtAuthentication(keycloakUrl, realm);
    }

    public static IServiceCollection AddPalantirJwtAuthentication(
        this IServiceCollection services,
        string keycloakUrl,
        string realm = "palantir")
    {
        var realmUrl = $"{keycloakUrl.TrimEnd('/')}/realms/{realm}";

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = realmUrl;
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = realmUrl,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        MapKeycloakRealmRoles(context.Principal);
                        return Task.CompletedTask;
                    }
                };
            });

        return services;
    }

    /// <summary>
    /// Extracts roles from Keycloak's nested <c>realm_access.roles</c> JSON claim into standard role claims
    /// so <see cref="ClaimsPrincipal.IsInRole(string)"/> works seamlessly across all services.
    /// </summary>
    public static void MapKeycloakRealmRoles(ClaimsPrincipal? principal)
    {
        if (principal is null)
        {
            return;
        }

        foreach (var identity in principal.Identities)
        {
            var realmAccess = identity.FindFirst("realm_access")?.Value;
            if (string.IsNullOrWhiteSpace(realmAccess))
            {
                continue;
            }

            try
            {
                using var doc = JsonDocument.Parse(realmAccess);
                if (!doc.RootElement.TryGetProperty("roles", out var rolesElement) ||
                    rolesElement.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var roleElement in rolesElement.EnumerateArray())
                {
                    if (roleElement.ValueKind != JsonValueKind.String)
                    {
                        continue;
                    }

                    var role = roleElement.GetString();
                    if (!string.IsNullOrWhiteSpace(role) && !identity.HasClaim(identity.RoleClaimType, role))
                    {
                        identity.AddClaim(new Claim(identity.RoleClaimType, role));
                    }
                }
            }
            catch (JsonException)
            {
                // Ignore malformed realm_access claim payloads
            }
        }
    }

    public static IHttpClientBuilder AddTokenExchangeHandler(this IHttpClientBuilder builder, string targetAudience)
    {
        return builder.AddHttpMessageHandler(sp => new TokenExchangeDelegatingHandler(
            sp.GetRequiredService<IHttpContextAccessor>(),
            sp.GetRequiredService<ITokenExchangeService>(),
            targetAudience));
    }
}
