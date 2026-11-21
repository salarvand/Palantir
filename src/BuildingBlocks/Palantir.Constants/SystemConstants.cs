namespace Palantir.Constants;

public static class AspireResourceNames
{
    // Infrastructure
    public const string Postgres = "postgres";
    public const string Timescale = "timescale";
    public const string Redis = "redis";
    public const string RabbitMq = "rabbitmq";
    public const string Kafka = "kafka";
    public const string Keycloak = "keycloak";
    public const string Elasticsearch = "elasticsearch";
    public const string Qdrant = "qdrant";

    // Databases
    public const string PalantirDb = "PalantirDb";
    public const string TimescaleDb = "TimescaleDb";

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
    public const string MockExchange = "mock-exchange";

    // Frontend
    public const string FrontendShell = "frontend-shell";
}

public static class MessagingConstants
{
    public static class KafkaTopics
    {
        public const string MarketTicks = "market.ticks.v1";
        public const string MarketCandles = "market.ohlc.v1";
        public const string TradesExecuted = "trades.executed.v1";
        public const string OrderBookLevel2 = "market.orderbook.v1";
    }

    public static class RabbitMqExchanges
    {
        public const string TradingCommands = "trading.commands";
        public const string Sagas = "trading.sagas";
        public const string IamEvents = "iam.events";
    }

    public static class RedisKeys
    {
        public static string PermissionToken(Guid userId) => $"iam:perm:{userId:N}";
        public static string OrderBook(string symbol) => $"market:book:{symbol.ToUpperInvariant()}";
        public static string CurrentCandle(string symbol, string timeframe) => $"market:candle:{symbol.ToUpperInvariant()}:{timeframe}";
    }
}
