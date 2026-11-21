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

builder.Build().Run();
