using Microsoft.AspNetCore.Http;

namespace Palantir.Platform.Security.Permissions;

/// <summary>
/// Runtime environmental attributes evaluated by permission specifications.
/// </summary>
public sealed record PermissionContext(
    DateTimeOffset EvaluatedAtUtc,
    string? SubjectUserId = null,
    string? ClientIpAddress = null,
    string? MarketState = null,
    bool? IsVpnConnected = null,
    string? TenantId = null,
    IReadOnlyDictionary<string, string>? CustomAttributes = null)
{
    public static PermissionContext Default() => new(DateTimeOffset.UtcNow);

    public static PermissionContext FromHttpContext(HttpContext? httpContext, string? subjectUserId = null)
    {
        if (httpContext is null)
        {
            return new PermissionContext(DateTimeOffset.UtcNow, subjectUserId);
        }

        return new PermissionContext(
            DateTimeOffset.UtcNow,
            subjectUserId,
            httpContext.Connection.RemoteIpAddress?.ToString(),
            httpContext.Request.Headers["X-Market-State"].FirstOrDefault(),
            httpContext.Request.Headers.TryGetValue("X-VPN-Connected", out var vpn) &&
            bool.TryParse(vpn.FirstOrDefault(), out var isVpn) && isVpn,
            httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault());
    }
}
