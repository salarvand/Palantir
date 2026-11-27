using System.Security.Claims;
using Palantir.Constants;
using Palantir.Platform.Security;
using Palantir.Platform.Security.Permissions;
using Shouldly;
using Xunit;

namespace Palantir.Platform.UnitTests;

public sealed class PermissionEvaluatorTests
{
    private sealed class InMemoryTokenManager(PermissionToken? token) : IPermissionTokenManager
    {
        public Task<PermissionToken?> GetPermissionTokenAsync(Guid userId, CancellationToken ct = default) => Task.FromResult(token);
        public Task SetPermissionTokenAsync(PermissionToken t, TimeSpan ttl, CancellationToken ct = default) => Task.CompletedTask;
        public Task InvalidatePermissionTokenAsync(Guid userId, CancellationToken ct = default) => Task.CompletedTask;
        public Task<bool> HasPermissionAsync(Guid userId, string requiredPermission, CancellationToken ct = default) =>
            Task.FromResult(token?.Permissions.Contains(requiredPermission, StringComparer.OrdinalIgnoreCase) ?? false);
    }

    private static PermissionEvaluator CreateEvaluator(PermissionToken? token) =>
        new(
            new InMemoryTokenManager(token),
            [
                new DimensionActionSpec(),
                new ResourceMatchSpec(),
                new OwnershipSpec(),
                new TradingHoursSpec(),
                new NetworkConditionSpec()
            ]);

    [Fact]
    public async Task EvaluateAsync_ShouldAllow_WhenFlatPermissionMatchesDimensionAndAction()
    {
        var userId = Guid.NewGuid();
        var token = new PermissionToken(userId, "trader", ["Trader"], [Permissions.AllowPlaceOrder], DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1));
        var evaluator = CreateEvaluator(token);

        var result = await evaluator.EvaluateAsync(userId, PermissionDimension.Orders, "Place", ResourceTemplate.Any("Order"));
        result.IsAllowed.ShouldBeTrue();
    }

    [Fact]
    public void EvaluateGrants_ShouldDeny_WhenVpnRequiredButNotConnected()
    {
        var evaluator = CreateEvaluator(null);
        var grants = new[]
        {
            new PermissionGrant(
                PermissionDimension.Risk,
                "OverrideLimits",
                "RiskLimit",
                Conditions: new Dictionary<string, string> { ["RequiresVpn"] = "true" })
        };

        var deniedContext = new PermissionContext(DateTimeOffset.UtcNow, IsVpnConnected: false);
        var allowedContext = new PermissionContext(DateTimeOffset.UtcNow, IsVpnConnected: true);

        evaluator.EvaluateGrants(grants, PermissionDimension.Risk, "OverrideLimits", ResourceTemplate.Any("RiskLimit"), deniedContext)
            .IsAllowed.ShouldBeFalse();
        evaluator.EvaluateGrants(grants, PermissionDimension.Risk, "OverrideLimits", ResourceTemplate.Any("RiskLimit"), allowedContext)
            .IsAllowed.ShouldBeTrue();
    }

    [Fact]
    public void MapKeycloakRealmRoles_ShouldPopulateRoleClaims_FromRealmAccessJson()
    {
        var identity = new ClaimsIdentity([new Claim("realm_access", """{"roles":["Admin","Trader"]}""")], "Bearer");
        var principal = new ClaimsPrincipal(identity);

        SecurityExtensions.MapKeycloakRealmRoles(principal);

        principal.IsInRole("Admin").ShouldBeTrue();
        principal.IsInRole("Trader").ShouldBeTrue();
    }

    [Fact]
    public void MapKeycloakRealmRoles_ShouldIgnoreMalformedPayload()
    {
        var identity = new ClaimsIdentity([new Claim("realm_access", "not-json")], "Bearer");
        var principal = new ClaimsPrincipal(identity);

        SecurityExtensions.MapKeycloakRealmRoles(principal);

        principal.IsInRole("Admin").ShouldBeFalse();
    }
}
