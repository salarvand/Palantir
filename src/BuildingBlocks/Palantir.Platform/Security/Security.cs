using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Palantir.Constants;
using Palantir.Platform.Caching;

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

        using var request = new HttpRequestMessage(HttpMethod.Post, "/protocol/openid-connect/token")
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
    private readonly IPermissionTokenManager _tokenManager;

    public PermissionAuthorizationHandler(IPermissionTokenManager tokenManager)
    {
        _tokenManager = tokenManager;
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

        var hasPermission = await _tokenManager.HasPermissionAsync(userId, requirement.Permission);
        if (hasPermission)
        {
            context.Succeed(requirement);
        }
    }
}

public static class SecurityExtensions
{
    public static IServiceCollection AddPalantirSecurity(this IServiceCollection services, string? keycloakBaseUrl = null)
    {
        services.AddSingleton<IPermissionTokenManager, PostgresPermissionTokenManager>();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

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
            foreach (var permission in Permissions.All)
            {
                options.AddPolicy(permission, policy => policy.Requirements.Add(new PermissionRequirement(permission)));
            }
        });

        return services;
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
            });

        return services;
    }
}
