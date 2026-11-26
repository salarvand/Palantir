using Palantir.Constants;
using Palantir.Platform;
using Palantir.Platform.Messaging;
using Palantir.RiskService.Engine;
using Palantir.RiskService.IntegrationEvents.Events;
using Palantir.ServiceDefaults;
using Palantir.ServiceDefaults.ApiSpecification.AsyncApi;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddDefaultAsyncApi("Palantir Risk Service", "1.0.0", typeof(Program));
builder.AddPalantirChassis(typeof(Program).Assembly);

var connectionString = builder.Configuration.GetConnectionString(AspireResourceNames.PalantirDb)
                       ?? "Host=localhost;Database=PalantirDb;Username=postgres;Password=postgres";
var rabbitHost = builder.Configuration.GetConnectionString(AspireResourceNames.RabbitMq)
                 ?? "amqp://guest:guest@localhost:5672";

builder.Services.AddSingleton<RiskEngine>();

builder.Host.AddPalantirMessaging(connectionString, rabbitHost, opts =>
{
    opts.ListenToRabbitQueue(MessagingConstants.RabbitMqQueues.RiskChecks);
    opts.PublishMessage<PreTradeRiskApprovedEvent>().ToRabbitExchange(MessagingConstants.RabbitMqExchanges.Sagas, e =>
    {
        e.ExchangeType = ExchangeType.Fanout;
        e.BindQueue(MessagingConstants.RabbitMqQueues.SettlementSaga);
        e.BindQueue(MessagingConstants.RabbitMqQueues.PortfolioMargin);
    });
    opts.PublishMessage<OrderSettlementFailedEvent>().ToRabbitQueue(MessagingConstants.RabbitMqQueues.SettlementSaga);
    opts.PublishMessage<AuditTrailEvent>().ToRabbitQueue(MessagingConstants.RabbitMqQueues.AuditTrail);
});

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseDefaultAsyncApi();
app.UsePalantirChassis();

app.Run();
