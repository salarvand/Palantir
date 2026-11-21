using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Palantir.Constants;
using StackExchange.Redis;

namespace Palantir.Platform.Security;

public record PermissionToken(
    Guid UserId,
    string Username,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset ExpiresAtUtc
);

public interface IPermissionTokenManager
{
    Task<PermissionToken?> GetPermissionTokenAsync(Guid userId, CancellationToken ct = default);
    Task SetPermissionTokenAsync(PermissionToken token, TimeSpan ttl, CancellationToken ct = default);
    Task InvalidatePermissionTokenAsync(Guid userId, CancellationToken ct = default);
    Task<bool> HasPermissionAsync(Guid userId, string requiredPermission, CancellationToken ct = default);
}

public class RedisPermissionTokenManager : IPermissionTokenManager
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisPermissionTokenManager> _logger;

    public RedisPermissionTokenManager(IConnectionMultiplexer redis, ILogger<RedisPermissionTokenManager> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    private IDatabase Db => _redis.GetDatabase();

    public async Task<PermissionToken?> GetPermissionTokenAsync(Guid userId, CancellationToken ct = default)
    {
        var key = MessagingConstants.RedisKeys.PermissionToken(userId);
        var json = await Db.StringGetAsync(key);
        if (json.IsNullOrEmpty) return null;

        try
        {
            return JsonSerializer.Deserialize<PermissionToken>((string)json!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize permission token for user {UserId}", userId);
            return null;
        }
    }

    public async Task SetPermissionTokenAsync(PermissionToken token, TimeSpan ttl, CancellationToken ct = default)
    {
        var key = MessagingConstants.RedisKeys.PermissionToken(token.UserId);
        var json = JsonSerializer.Serialize(token);
        await Db.StringSetAsync(key, json, ttl);
    }

    public async Task InvalidatePermissionTokenAsync(Guid userId, CancellationToken ct = default)
    {
        var key = MessagingConstants.RedisKeys.PermissionToken(userId);
        await Db.KeyDeleteAsync(key);
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
    public static IServiceCollection AddPalantirSecurity(this IServiceCollection services)
    {
        services.AddSingleton<IPermissionTokenManager, RedisPermissionTokenManager>();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

        services.AddAuthorization(options =>
        {
            foreach (var permission in Permissions.All)
            {
                options.AddPolicy(permission, policy => policy.Requirements.Add(new PermissionRequirement(permission)));
            }
        });

        return services;
    }
}
