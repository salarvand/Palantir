using Microsoft.EntityFrameworkCore;
using Palantir.Constants;
using Palantir.IamService.Infrastructure;
using Palantir.IamService.IntegrationEvents.Events;
using Palantir.Platform;
using Palantir.Platform.Messaging;
using Palantir.Platform.Persistence;
using Palantir.ServiceDefaults;
using Palantir.ServiceDefaults.ApiSpecification.AsyncApi;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddDefaultAsyncApi("Palantir IAM Service", "1.0.0", typeof(Program));
builder.AddPalantirChassis(typeof(Program).Assembly);

var connectionString = builder.Configuration.GetConnectionString(AspireResourceNames.PalantirDb)
                       ?? "Host=localhost;Database=PalantirDb;Username=postgres;Password=postgres;SSL Mode=Disable;Include Error Detail=true";
var rabbitHost = builder.Configuration.GetConnectionString(AspireResourceNames.RabbitMq)
                 ?? "amqp://guest:guest@localhost:5672";

builder.Services.AddDbContext<IamDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsql =>
    {
        npgsql.EnableRetryOnFailure(10, TimeSpan.FromSeconds(5), null);
    });
});

builder.Services.AddHttpClient<IKeycloakAdminClient, KeycloakAdminClient>();

builder.Host.AddPalantirMessaging(connectionString, rabbitHost, opts =>
{
    opts.PublishMessage<UserCreatedIntegrationEvent>().ToRabbitExchange(MessagingConstants.RabbitMqExchanges.IamEvents, e =>
    {
        e.ExchangeType = ExchangeType.Fanout;
        e.BindQueue(MessagingConstants.RabbitMqQueues.IamEvents);
    });
    opts.PublishMessage<PermissionsUpdatedIntegrationEvent>().ToRabbitExchange(MessagingConstants.RabbitMqExchanges.IamEvents, e =>
    {
        e.ExchangeType = ExchangeType.Fanout;
        e.BindQueue(MessagingConstants.RabbitMqQueues.IamEvents);
    });
}, useEntityFrameworkCore: true);

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseDefaultAsyncApi();

await app.Services.EnsurePostgresSchemaCreatedAsync<IamDbContext>(
    "iam",
    app.Services.GetRequiredService<ILogger<Program>>());

app.UsePalantirChassis();

app.Run();
