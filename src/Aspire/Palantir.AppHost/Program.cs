using Aspire.Hosting;
using Palantir.Constants;

var builder = DistributedApplication.CreateBuilder(args);

// Consolidated PostgreSQL (pgvector/pg16) & RabbitMQ Infrastructure
var postgres = builder.AddPostgres(AspireResourceNames.Postgres)
    .WithImage("pgvector/pgvector", "pg16")
    .WithDataVolume()
    .WithPgAdmin();

var palantirDb = postgres.AddDatabase(AspireResourceNames.PalantirDb);

var rabbitmq = builder.AddRabbitMQ(AspireResourceNames.RabbitMq)
    .WithManagementPlugin()
    .WithDataVolume();

// Keycloak Container with Automated Realm Import
var realmPath = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "../../../deploy/keycloak/palantir-realm.json"));
var keycloak = builder.AddContainer(AspireResourceNames.Keycloak, "quay.io/keycloak/keycloak", "26.0")
    .WithHttpEndpoint(port: 8080, targetPort: 8080, name: "http")
    .WithEnvironment("KEYCLOAK_ADMIN", "admin")
    .WithEnvironment("KEYCLOAK_ADMIN_PASSWORD", "admin123")
    .WithBindMount(realmPath, "/opt/keycloak/data/import/palantir-realm.json")
    .WithArgs("start-dev", "--import-realm");

// Microservices & Edge Projects
var iamService = builder.AddProject<Projects.Palantir_IamService>(AspireResourceNames.IamService)
    .WithReference(palantirDb)
    .WithReference(rabbitmq)
    .WithReference(keycloak.GetEndpoint("http"))
    .WithEnvironment("Keycloak__Url", keycloak.GetEndpoint("http"))
    .WaitFor(palantirDb)
    .WaitFor(rabbitmq);

var localEngine = builder.AddProject<Projects.Palantir_LocalEngineService>(AspireResourceNames.LocalEngineService)
    .WithReference(palantirDb)
    .WithReference(rabbitmq)
    .WaitFor(palantirDb)
    .WaitFor(rabbitmq);

var externalGateway = builder.AddProject<Projects.Palantir_ExternalGatewayService>(AspireResourceNames.ExternalGatewayService)
    .WithReference(palantirDb)
    .WithReference(rabbitmq)
    .WaitFor(palantirDb)
    .WaitFor(rabbitmq);

var omsService = builder.AddProject<Projects.Palantir_OmsService>(AspireResourceNames.OmsService)
    .WithReference(palantirDb)
    .WithReference(rabbitmq)
    .WithReference(localEngine)
    .WithReference(externalGateway)
    .WaitFor(palantirDb)
    .WaitFor(rabbitmq)
    .WaitFor(localEngine);

var marketData = builder.AddProject<Projects.Palantir_MarketDataService>(AspireResourceNames.MarketDataService)
    .WithReference(palantirDb)
    .WithReference(rabbitmq)
    .WaitFor(palantirDb)
    .WaitFor(rabbitmq);

var riskService = builder.AddProject<Projects.Palantir_RiskService>(AspireResourceNames.RiskService)
    .WithReference(palantirDb)
    .WithReference(rabbitmq)
    .WaitFor(palantirDb)
    .WaitFor(rabbitmq);

var portfolioService = builder.AddProject<Projects.Palantir_PortfolioService>(AspireResourceNames.PortfolioService)
    .WithReference(palantirDb)
    .WithReference(rabbitmq)
    .WaitFor(palantirDb)
    .WaitFor(rabbitmq);

var settlementService = builder.AddProject<Projects.Palantir_SettlementService>(AspireResourceNames.SettlementService)
    .WithReference(palantirDb)
    .WithReference(rabbitmq)
    .WaitFor(palantirDb)
    .WaitFor(rabbitmq);

var auditService = builder.AddProject<Projects.Palantir_AuditService>(AspireResourceNames.AuditService)
    .WithReference(palantirDb)
    .WithReference(rabbitmq)
    .WaitFor(palantirDb)
    .WaitFor(rabbitmq);

var aiAssistant = builder.AddProject<Projects.Palantir_AiAssistantService>(AspireResourceNames.AiAssistantService)
    .WithReference(palantirDb)
    .WithReference(omsService)
    .WithReference(marketData)
    .WithReference(riskService)
    .WithReference(portfolioService)
    .WaitFor(palantirDb);

foreach (var key in new[] { "ApiKey", "Endpoint", "ChatModel", "EmbeddingModel" })
{
    var value = builder.Configuration[$"AiProvider:{key}"];
    if (!string.IsNullOrWhiteSpace(value))
    {
        aiAssistant.WithEnvironment($"AiProvider__{key}", value);
    }
}

var gateway = builder.AddProject<Projects.Palantir_Gateway>(AspireResourceNames.Gateway)
    .WithReference(iamService)
    .WithReference(omsService)
    .WithReference(localEngine)
    .WithReference(externalGateway)
    .WithReference(marketData)
    .WithReference(riskService)
    .WithReference(portfolioService)
    .WithReference(settlementService)
    .WithReference(auditService)
    .WithReference(aiAssistant);

builder.Build().Run();
