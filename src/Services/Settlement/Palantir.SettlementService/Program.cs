using Microsoft.EntityFrameworkCore;
using Palantir.Constants;
using Palantir.Platform;
using Palantir.Platform.Messaging;
using Palantir.Platform.Persistence;
using Palantir.ServiceDefaults;
using Palantir.ServiceDefaults.ApiSpecification.AsyncApi;
using Palantir.SettlementService.Infrastructure;
using Palantir.SettlementService.IntegrationEvents.Events;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddDefaultAsyncApi("Palantir Settlement Service", "1.0.0", typeof(Program));
builder.AddPalantirChassis(typeof(Program).Assembly);

var connectionString = builder.Configuration.GetConnectionString(AspireResourceNames.PalantirDb)
                       ?? "Host=localhost;Database=PalantirDb;Username=postgres;Password=postgres";
var rabbitHost = builder.Configuration.GetConnectionString(AspireResourceNames.RabbitMq)
                 ?? "amqp://guest:guest@localhost:5672";

builder.Services.AddDbContext<SettlementDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsql =>
    {
        npgsql.EnableRetryOnFailure(10, TimeSpan.FromSeconds(5), null);
    });
});

builder.Host.AddPalantirMessaging(connectionString, rabbitHost, opts =>
{
    opts.ListenToRabbitQueue(MessagingConstants.RabbitMqQueues.SettlementSaga);
    opts.PublishMessage<CheckPreTradeRiskCommand>().ToRabbitQueue(MessagingConstants.RabbitMqQueues.RiskChecks);
    opts.PublishMessage<ConfirmSettlementEvent>().ToRabbitQueue(MessagingConstants.RabbitMqQueues.PortfolioMargin);
    opts.PublishMessage<ReleaseReservedMarginCommand>().ToRabbitQueue(MessagingConstants.RabbitMqQueues.PortfolioMargin);
    opts.PublishMessage<AuditTrailEvent>().ToRabbitQueue(MessagingConstants.RabbitMqQueues.AuditTrail);
}, useEntityFrameworkCore: true);

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseDefaultAsyncApi();

await app.Services.EnsurePostgresSchemaCreatedAsync<SettlementDbContext>(
    "settlement",
    app.Services.GetRequiredService<ILogger<Program>>());

app.UsePalantirChassis();

app.Run();
