using Microsoft.EntityFrameworkCore;
using Palantir.Constants;
using Palantir.Oms.Contracts;
using Palantir.Platform.Caching;
using Palantir.Platform.Exceptions;
using Palantir.Platform.Messaging;
using Palantir.Platform.Persistence;
using Palantir.Platform.Security;
using Palantir.ServiceDefaults;
using Palantir.SettlementService.Infrastructure;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

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

builder.Services.AddPalantirCaching(connectionString);
builder.Services.AddPalantirSecurity();
builder.Services.AddPalantirProblemDetails();

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
app.UsePalantirExceptionHandler();

await app.Services.EnsurePostgresSchemaCreatedAsync<SettlementDbContext>(
    "settlement",
    app.Services.GetRequiredService<ILogger<Program>>());

app.MapGet("/api/v1/settlements/{orderId:guid}", async (Guid orderId, SettlementDbContext db, CancellationToken ct) =>
{
    var state = await db.SettlementStates.AsNoTracking().FirstOrDefaultAsync(s => s.Id == orderId, ct);
    return state != null ? Results.Ok(state) : Results.NotFound();
});

app.Run();
