using PermissionConstants = Palantir.Constants.Permissions;

namespace Palantir.Platform.Security.Permissions;

/// <summary>
/// Functional authorization dimension mapped to Keycloak resource groups.
/// </summary>
public sealed record PermissionDimension(string Name)
{
    public static readonly PermissionDimension Orders = new(PermissionConstants.DimensionNames.Orders);
    public static readonly PermissionDimension MarketData = new(PermissionConstants.DimensionNames.MarketData);
    public static readonly PermissionDimension Risk = new(PermissionConstants.DimensionNames.Risk);
    public static readonly PermissionDimension Portfolio = new(PermissionConstants.DimensionNames.Portfolio);
    public static readonly PermissionDimension Settlement = new(PermissionConstants.DimensionNames.Settlement);
    public static readonly PermissionDimension Audit = new(PermissionConstants.DimensionNames.Audit);
    public static readonly PermissionDimension Iam = new(PermissionConstants.DimensionNames.Iam);
    public static readonly PermissionDimension System = new(PermissionConstants.DimensionNames.System);

    public static PermissionDimension FromName(string name) =>
        name.Equals("Market", StringComparison.OrdinalIgnoreCase)
            ? MarketData
            : new PermissionDimension(name);
}
