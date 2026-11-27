namespace Palantir.Platform.Security.Permissions;

/// <summary>
/// Verifies that the grant's dimension and action cover the requested operation.
/// </summary>
public sealed class DimensionActionSpec : IPermissionSpecification
{
    public string RuleName => "DimensionAction";
    public string ErrorMessage => "Permission dimension or action mismatch.";

    public bool IsSatisfiedBy(PermissionEvaluationRequest req) =>
        string.Equals(req.Grant.Dimension.Name, req.Dimension.Name, StringComparison.OrdinalIgnoreCase) &&
        (req.Grant.Action == "*" ||
         req.Grant.Action.Equals(req.Action, StringComparison.OrdinalIgnoreCase));
}

/// <summary>
/// Verifies that the grant's resource scope matches the target resource template.
/// </summary>
public sealed class ResourceMatchSpec : IPermissionSpecification
{
    public string RuleName => "ResourceMatch";
    public string ErrorMessage => "Resource does not match grant scope.";

    public bool IsSatisfiedBy(PermissionEvaluationRequest req) =>
        req.Grant.ResourceType is null ||
        (req.Grant.ResourceType.Equals(req.Resource.Type, StringComparison.OrdinalIgnoreCase) &&
         (req.Grant.ResourceId is null ||
          req.Grant.ResourceId.Equals(req.Resource.Id, StringComparison.OrdinalIgnoreCase)));
}

/// <summary>
/// Enforces ownership when a grant specifies <c>RequireOwner=true</c>.
/// </summary>
public sealed class OwnershipSpec : IPermissionSpecification
{
    public string RuleName => "Ownership";
    public string ErrorMessage => "Subject must own the target resource.";

    public bool IsSatisfiedBy(PermissionEvaluationRequest req)
    {
        if (req.Grant.Conditions is null ||
            !req.Grant.Conditions.TryGetValue("RequireOwner", out var requireOwner) ||
            !bool.TryParse(requireOwner, out var mustOwn) ||
            !mustOwn)
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(req.Resource.OwnerId) &&
               string.Equals(req.Resource.OwnerId, req.Context.SubjectUserId, StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Enforces market trading-hours constraints when <c>MarketState</c> is present in grant conditions.
/// </summary>
public sealed class TradingHoursSpec : IPermissionSpecification
{
    public string RuleName => "TradingHours";
    public string ErrorMessage => "Operation is not permitted in the current market state.";

    public bool IsSatisfiedBy(PermissionEvaluationRequest req)
    {
        if (req.Grant.Conditions is null ||
            !req.Grant.Conditions.TryGetValue("MarketState", out var requiredState))
        {
            return true;
        }

        return req.Context.MarketState is not null &&
               req.Context.MarketState.Equals(requiredState, StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Enforces VPN / network conditions when <c>RequiresVpn=true</c> is configured on the grant.
/// </summary>
public sealed class NetworkConditionSpec : IPermissionSpecification
{
    public string RuleName => "NetworkCondition";
    public string ErrorMessage => "Required secure network (VPN) condition not satisfied.";

    public bool IsSatisfiedBy(PermissionEvaluationRequest req)
    {
        if (req.Grant.Conditions is null ||
            !req.Grant.Conditions.TryGetValue("RequiresVpn", out var vpnRequired) ||
            !bool.TryParse(vpnRequired, out var mustUseVpn) ||
            !mustUseVpn)
        {
            return true;
        }

        return req.Context.IsVpnConnected ?? false;
    }
}
