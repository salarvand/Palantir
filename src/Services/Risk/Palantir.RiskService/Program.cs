using Palantir.Constants;
using Palantir.Oms.Contracts;
using Palantir.Platform.Caching;
using Palantir.Platform.Endpoints;
using Palantir.Platform.Exceptions;
using Palantir.Platform.Messaging;
using Palantir.Platform.Security;
using Palantir.RiskService.Engine;
using Palantir.ServiceDefaults;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var connectionString = builder.Configuration.GetConnectionString(AspireResourceNames.PalantirDb)
                       ?? "Host=localhost;Database=PalantirDb;Username=postgres;Password=postgres";
var rabbitHost = builder.Configuration.GetConnectionString(AspireResourceNames.RabbitMq)
                 ?? "amqp://guest:guest@localhost:5672";

builder.Services.AddPalantirCaching(connectionString);
builder.Services.AddPalantirSecurity();
builder.Services.AddPalantirProblemDetails();
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

builder.Services.AddEndpoints(typeof(Program).Assembly);

var app = builder.Build();

app.MapDefaultEndpoints();
app.UsePalantirExceptionHandler();
app.MapEndpoints();

app.Run();
