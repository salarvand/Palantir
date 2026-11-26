namespace Palantir.Constants;

public static class AspireResourceNames
{
    // Infrastructure
    public const string Postgres = "postgres";
    public const string RabbitMq = "rabbitmq";
    public const string Keycloak = "keycloak";

    // Databases
    public const string PalantirDb = "PalantirDb";

    // Microservices
    public const string Gateway = "gateway";
    public const string IamService = "iam-service";
    public const string OmsService = "oms-service";
    public const string LocalEngineService = "local-engine-service";
    public const string ExternalGatewayService = "external-gateway-service";
    public const string MarketDataService = "market-data-service";
    public const string RiskService = "risk-service";
    public const string PortfolioService = "portfolio-service";
    public const string SettlementService = "settlement-service";
    public const string AuditService = "audit-service";
    public const string AiAssistantService = "ai-assistant-service";

    // Frontend Microfrontends & Next.js Multi-Zones
    public const string FrontendShell = "frontend-shell";
    public const string TradingDeskMfe = "trading-desk-mfe";
    public const string PortfolioMfe = "portfolio-mfe";
    public const string AdminMfe = "admin-mfe";
    public const string StorefrontZone = "storefront-zone";
    public const string BackofficeZone = "backoffice-zone";
}

public static class ApiConstants
{
    public const string RequestIdHeader = "x-request-id";
    public const string CorrelationIdHeader = "x-correlation-id";
}

public static class MessagingConstants
{
    public static class RabbitMqExchanges
    {
        public const string TradingCommands = "trading.commands";
        public const string Sagas = "trading.sagas";
        public const string IamEvents = "iam.events";
        public const string MarketData = "market.data";
        public const string AuditEvents = "audit.events";
    }

    public static class RabbitMqQueues
    {
        public const string MarketTicks = "market.ticks.v1";
        public const string OrderBookUpdates = "market.orderbook.v1";
        public const string SettlementSaga = "trading.settlement.saga";
        public const string RiskChecks = "trading.risk.checks";
        public const string PortfolioMargin = "trading.portfolio.margin";
        public const string AuditTrail = "audit.trail.v1";
        public const string IamEvents = "iam.events.v1";
    }

    public static class CacheKeys
    {
        public static string PermissionToken(Guid userId) => $"iam:perm:{userId:N}";
        public static string OrderBook(string symbol) => $"market:book:{symbol.ToUpperInvariant()}";
        public static string MarketQuote(string symbol) => $"market:quote:{symbol.ToUpperInvariant()}";
        public static string TokenExchange(string targetAudience, string tokenHash) => $"iam:token_exchange:{targetAudience}:{tokenHash}";
    }
}
