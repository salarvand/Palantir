using Microsoft.EntityFrameworkCore;
using Palantir.Constants;
using Palantir.Iam.Contracts;
using Palantir.IamService.Infrastructure;
using Palantir.Platform.Caching;
using Palantir.Platform.Endpoints;
using Palantir.Platform.Exceptions;
using Palantir.Platform.Messaging;
using Palantir.Platform.Persistence;
using Palantir.Platform.Security;
using Palantir.ServiceDefaults;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

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

builder.Services.AddPalantirCaching(connectionString);
builder.Services.AddPalantirSecurity();
builder.Services.AddPalantirProblemDetails();
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

builder.Services.AddEndpoints(typeof(Program).Assembly);

var app = builder.Build();

app.MapDefaultEndpoints();
app.UsePalantirExceptionHandler();

await app.Services.EnsurePostgresSchemaCreatedAsync<IamDbContext>(
    "iam",
    app.Services.GetRequiredService<ILogger<Program>>());

app.MapEndpoints();

app.Run();
