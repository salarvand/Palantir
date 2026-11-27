namespace Palantir.Constants;

public static class Permissions
{
    // Orders
    public const string AllowPlaceOrder = "Permissions.Orders.Place";
    public const string AllowCancelOrder = "Permissions.Orders.Cancel";
    public const string AllowModifyOrder = "Permissions.Orders.Modify";
    public const string AllowViewBlotter = "Permissions.Orders.ViewBlotter";
    public const string AllowExportTrades = "Permissions.Orders.ExportTrades";

    // Symbols & Market Data
    public const string AllowDefineSymbol = "Permissions.Market.DefineSymbol";
    public const string AllowModifySymbol = "Permissions.Market.ModifySymbol";
    public const string AllowViewLevel2Data = "Permissions.Market.ViewL2Data";

    // Risk & Compliance
    public const string AllowOverrideRisk = "Permissions.Risk.OverrideLimits";
    public const string AllowViewRiskExposure = "Permissions.Risk.ViewExposure";
    public const string AllowManageRiskLimits = "Permissions.Risk.ManageLimits";
    public const string AllowHaltTrading = "Permissions.Risk.HaltTrading";

    // Portfolio & Settlement
    public const string AllowViewPortfolio = "Permissions.Portfolio.View";
    public const string AllowAdjustBalance = "Permissions.Portfolio.AdjustBalance";
    public const string AllowInitiateSettlement = "Permissions.Settlement.Initiate";

    // Audit & Administration
    public const string AllowViewAuditLogs = "Permissions.Audit.View";
    public const string AllowExportAuditLogs = "Permissions.Audit.Export";
    public const string AllowManageUsers = "Permissions.Iam.ManageUsers";
    public const string AllowManageRoles = "Permissions.Iam.ManageRoles";
    public const string AllowViewSystemHealth = "Permissions.System.ViewHealth";

    public static readonly IReadOnlyList<string> All =
    [
        AllowPlaceOrder, AllowCancelOrder, AllowModifyOrder, AllowViewBlotter, AllowExportTrades,
        AllowDefineSymbol, AllowModifySymbol, AllowViewLevel2Data,
        AllowOverrideRisk, AllowViewRiskExposure, AllowManageRiskLimits, AllowHaltTrading,
        AllowViewPortfolio, AllowAdjustBalance, AllowInitiateSettlement,
        AllowViewAuditLogs, AllowExportAuditLogs, AllowManageUsers, AllowManageRoles, AllowViewSystemHealth
    ];

    public static class DimensionNames
    {
        public const string Orders = "Orders";
        public const string MarketData = "MarketData";
        public const string Risk = "Risk";
        public const string Portfolio = "Portfolio";
        public const string Settlement = "Settlement";
        public const string Audit = "Audit";
        public const string Iam = "Iam";
        public const string System = "System";
    }
}
