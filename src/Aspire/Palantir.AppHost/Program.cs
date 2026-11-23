using Aspire.Hosting;
using Palantir.Constants;

var builder = DistributedApplication.CreateBuilder(args);

// Storage & Infrastructure Containers
var postgres = builder.AddPostgres(AspireResourceNames.Postgres)
    .WithDataVolume()
    .WithPgAdmin();

var palantirDb = postgres.AddDatabase(AspireResourceNames.PalantirDb);

var redis = builder.AddRedis(AspireResourceNames.Redis)
    .WithDataVolume();

var rabbitmq = builder.AddRabbitMQ(AspireResourceNames.RabbitMq)
    .WithManagementPlugin()
    .WithDataVolume();

var kafka = builder.AddKafka(AspireResourceNames.Kafka)
    .WithDataVolume();

// Keycloak Container with Automated Realm Import
var keycloak = builder.AddContainer(AspireResourceNames.Keycloak, "quay.io/keycloak/keycloak", "26.0")
    .WithHttpEndpoint(port: 8080, targetPort: 8080, name: "http")
    .WithEnvironment("KEYCLOAK_ADMIN", "admin")
    .WithEnvironment("KEYCLOAK_ADMIN_PASSWORD", "admin123")
    .WithBindMount("../../deploy/keycloak/palantir-realm.json", "/opt/keycloak/data/import/palantir-realm.json")
    .WithArgs("start-dev", "--import-realm");

// Polyglot Persistence Containers
var timescale = builder.AddContainer(AspireResourceNames.Timescale, "timescale/timescaledb-ha", "pg16")
    .WithEndpoint(port: 5433, targetPort: 5432, name: "tcp")
    .WithEnvironment("POSTGRES_DB", AspireResourceNames.TimescaleDb)
    .WithEnvironment("POSTGRES_USER", "postgres")
    .WithEnvironment("POSTGRES_PASSWORD", "postgres")
    .WithVolume("timescaledb-data", "/var/lib/postgresql/data");

var elasticsearch = builder.AddContainer(AspireResourceNames.Elasticsearch, "docker.elastic.co/elasticsearch/elasticsearch", "8.17.0")
    .WithHttpEndpoint(port: 9200, targetPort: 9200, name: "http")
    .WithEnvironment("discovery.type", "single-node")
    .WithEnvironment("xpack.security.enabled", "false")
    .WithVolume("elasticsearch-data", "/usr/share/elasticsearch/data");

var qdrant = builder.AddContainer(AspireResourceNames.Qdrant, "qdrant/qdrant", "latest")
    .WithHttpEndpoint(port: 6333, targetPort: 6333, name: "http")
    .WithEndpoint(port: 6334, targetPort: 6334, name: "grpc")
    .WithVolume("qdrant-data", "/qdrant/storage");

// Ollama Container Registration (Zero Azure)
var ollama = builder.AddContainer(AspireResourceNames.Ollama, "ollama/ollama", "latest")
    .WithHttpEndpoint(port: 11434, targetPort: 11434, name: "http")
    .WithVolume("ollama-models", "/root/.ollama");

// Microservices & Edge Projects
var iamService = builder.AddProject<Projects.Palantir_IamService>(AspireResourceNames.IamService)
    .WithReference(palantirDb)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WithReference(keycloak.GetEndpoint("http"))
    .WithEnvironment("Keycloak__Url", keycloak.GetEndpoint("http"))
    .WaitFor(palantirDb);

var localEngine = builder.AddProject<Projects.Palantir_LocalEngineService>(AspireResourceNames.LocalEngineService)
    .WithReference(palantirDb)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WithReference(kafka)
    .WithReference(timescale.GetEndpoint("tcp"))
    .WithEnvironment("ConnectionStrings__TimescaleDb", "Host=localhost;Port=5433;Database=TimescaleDb;Username=postgres;Password=postgres")
    .WaitFor(palantirDb);

var externalGateway = builder.AddProject<Projects.Palantir_ExternalGatewayService>(AspireResourceNames.ExternalGatewayService)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WithReference(kafka);

var omsService = builder.AddProject<Projects.Palantir_OmsService>(AspireResourceNames.OmsService)
    .WithReference(palantirDb)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WithReference(localEngine)
    .WithReference(externalGateway)
    .WaitFor(palantirDb);

var marketData = builder.AddProject<Projects.Palantir_MarketDataService>(AspireResourceNames.MarketDataService)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WithReference(kafka);

var riskService = builder.AddProject<Projects.Palantir_RiskService>(AspireResourceNames.RiskService)
    .WithReference(redis)
    .WithReference(rabbitmq);

var portfolioService = builder.AddProject<Projects.Palantir_PortfolioService>(AspireResourceNames.PortfolioService)
    .WithReference(palantirDb)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WaitFor(palantirDb);

var settlementService = builder.AddProject<Projects.Palantir_SettlementService>(AspireResourceNames.SettlementService)
    .WithReference(palantirDb)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WaitFor(palantirDb);

var auditService = builder.AddProject<Projects.Palantir_AuditService>(AspireResourceNames.AuditService)
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WithReference(elasticsearch.GetEndpoint("http"))
    .WithEnvironment("Elasticsearch__Url", elasticsearch.GetEndpoint("http"));

var aiAssistant = builder.AddProject<Projects.Palantir_AiAssistantService>(AspireResourceNames.AiAssistantService)
    .WithReference(redis)
    .WithReference(qdrant.GetEndpoint("grpc"))
    .WithReference(ollama.GetEndpoint("http"))
    .WithReference(omsService)
    .WithReference(marketData)
    .WithReference(riskService)
    .WithReference(portfolioService)
    .WithEnvironment("Qdrant__Endpoint", qdrant.GetEndpoint("grpc"))
    .WithEnvironment("Ollama__Endpoint", ollama.GetEndpoint("http"));

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
