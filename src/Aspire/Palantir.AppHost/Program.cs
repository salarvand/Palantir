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

var keycloakHttp = keycloak.GetEndpoint("http");

// Microservices & Edge Projects (all receive Keycloak__Url for JWT AuthN + RFC 8693 Token Exchange)
var iamService = builder.AddProject<Projects.Palantir_IamService>(AspireResourceNames.IamService)
    .WithReference(palantirDb)
    .WithReference(rabbitmq)
    .WithReference(keycloakHttp)
    .WithEnvironment("Keycloak__Url", keycloakHttp)
    .WaitFor(palantirDb)
    .WaitFor(rabbitmq);

var localEngine = builder.AddProject<Projects.Palantir_LocalEngineService>(AspireResourceNames.LocalEngineService)
    .WithReference(palantirDb)
    .WithReference(rabbitmq)
    .WithEnvironment("Keycloak__Url", keycloakHttp)
    .WaitFor(palantirDb)
    .WaitFor(rabbitmq);

var externalGateway = builder.AddProject<Projects.Palantir_ExternalGatewayService>(AspireResourceNames.ExternalGatewayService)
    .WithReference(palantirDb)
    .WithReference(rabbitmq)
    .WithEnvironment("Keycloak__Url", keycloakHttp)
    .WaitFor(palantirDb)
    .WaitFor(rabbitmq);

var omsService = builder.AddProject<Projects.Palantir_OmsService>(AspireResourceNames.OmsService)
    .WithReference(palantirDb)
    .WithReference(rabbitmq)
    .WithReference(localEngine)
    .WithReference(externalGateway)
    .WithEnvironment("Keycloak__Url", keycloakHttp)
    .WaitFor(palantirDb)
    .WaitFor(rabbitmq)
    .WaitFor(localEngine);

var marketData = builder.AddProject<Projects.Palantir_MarketDataService>(AspireResourceNames.MarketDataService)
    .WithReference(palantirDb)
    .WithReference(rabbitmq)
    .WithEnvironment("Keycloak__Url", keycloakHttp)
    .WaitFor(palantirDb)
    .WaitFor(rabbitmq);

var riskService = builder.AddProject<Projects.Palantir_RiskService>(AspireResourceNames.RiskService)
    .WithReference(palantirDb)
    .WithReference(rabbitmq)
    .WithEnvironment("Keycloak__Url", keycloakHttp)
    .WaitFor(palantirDb)
    .WaitFor(rabbitmq);

var portfolioService = builder.AddProject<Projects.Palantir_PortfolioService>(AspireResourceNames.PortfolioService)
    .WithReference(palantirDb)
    .WithReference(rabbitmq)
    .WithEnvironment("Keycloak__Url", keycloakHttp)
    .WaitFor(palantirDb)
    .WaitFor(rabbitmq);

var settlementService = builder.AddProject<Projects.Palantir_SettlementService>(AspireResourceNames.SettlementService)
    .WithReference(palantirDb)
    .WithReference(rabbitmq)
    .WithEnvironment("Keycloak__Url", keycloakHttp)
    .WaitFor(palantirDb)
    .WaitFor(rabbitmq);

var auditService = builder.AddProject<Projects.Palantir_AuditService>(AspireResourceNames.AuditService)
    .WithReference(palantirDb)
    .WithReference(rabbitmq)
    .WithEnvironment("Keycloak__Url", keycloakHttp)
    .WaitFor(palantirDb)
    .WaitFor(rabbitmq);

var aiAssistant = builder.AddProject<Projects.Palantir_AiAssistantService>(AspireResourceNames.AiAssistantService)
    .WithReference(palantirDb)
    .WithReference(omsService)
    .WithReference(marketData)
    .WithReference(riskService)
    .WithReference(portfolioService)
    .WithEnvironment("Keycloak__Url", keycloakHttp)
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
    .WithReference(aiAssistant)
    .WithEnvironment("Keycloak__Url", keycloakHttp);

// Frontend UIs Accessible via Aspire Dashboard (direct npm dev runners without per-app *-installer race/deadlocks):
var npmCommand = OperatingSystem.IsWindows() ? "npm.cmd" : "npm";

IResourceBuilder<ExecutableResource> AddFrontendUi(string name, string appFolder, int port) =>
    builder.AddExecutable(name, npmCommand, $"../../Clients/{appFolder}", "run", "dev")
        .WithHttpEndpoint(port: port, env: "PORT", isProxied: false)
        .WithReference(gateway)
        .WithEnvironment("NEXT_PUBLIC_GATEWAY_URL", gateway.GetEndpoint("http"))
        .WithEnvironment("NEXT_PUBLIC_KEYCLOAK_URL", keycloakHttp);

// 1. Webpack 5 Module Federation Remotes & Host Shell (:3000 - :3003)
var tradingDeskMfe = AddFrontendUi(AspireResourceNames.TradingDeskMfe, "trading-desk", 3001);
var portfolioMfe = AddFrontendUi(AspireResourceNames.PortfolioMfe, "portfolio-mfe", 3002);
var adminMfe = AddFrontendUi(AspireResourceNames.AdminMfe, "admin-mfe", 3003);

var frontendShell = AddFrontendUi(AspireResourceNames.FrontendShell, "shell", 3000)
    .WithReference(tradingDeskMfe.GetEndpoint("http"))
    .WithReference(portfolioMfe.GetEndpoint("http"))
    .WithReference(adminMfe.GetEndpoint("http"));

// 2. Next.js Multi-Zones: Storefront (:3100) & Backoffice (:3101)
var backofficeZone = AddFrontendUi(AspireResourceNames.BackofficeZone, "backoffice", 3101)
    .WithReference(frontendShell.GetEndpoint("http"))
    .WithEnvironment("MFE_SHELL_URL", frontendShell.GetEndpoint("http"));

AddFrontendUi(AspireResourceNames.StorefrontZone, "storefront", 3100)
    .WithReference(frontendShell.GetEndpoint("http"))
    .WithReference(backofficeZone.GetEndpoint("http"))
    .WithEnvironment("MFE_SHELL_URL", frontendShell.GetEndpoint("http"))
    .WithEnvironment("BACKOFFICE_URL", backofficeZone.GetEndpoint("http"));

builder.Build().Run();
