using Microsoft.EntityFrameworkCore;
using Palantir.Constants;
using Palantir.LocalEngineService.Grpc;
using Palantir.LocalEngineService.IntegrationEvents.Events;
using Palantir.LocalEngineService.Matching;
using Palantir.LocalEngineService.Persistence;
using Palantir.Platform;
using Palantir.Platform.Messaging;
using Palantir.Platform.Persistence;
using Palantir.ServiceDefaults;
using Palantir.ServiceDefaults.ApiSpecification.AsyncApi;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddDefaultAsyncApi("Palantir Local Engine Service", "1.0.0", typeof(Program));
builder.AddPalantirChassis(typeof(Program).Assembly);
builder.Services.AddGrpc();
builder.Services.AddGrpcHealthChecks();

var connectionString = builder.Configuration.GetConnectionString(AspireResourceNames.PalantirDb)
                       ?? "Host=localhost;Database=PalantirDb;Username=postgres;Password=postgres";
var rabbitHost = builder.Configuration.GetConnectionString(AspireResourceNames.RabbitMq)
                 ?? "amqp://guest:guest@localhost:5672";

builder.Services.AddDbContext<MarketTickDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsql =>
    {
        npgsql.EnableRetryOnFailure(10, TimeSpan.FromSeconds(5), null);
    });
});

builder.Services.AddSingleton<IOrderBookRegistry, InMemoryOrderBookRegistry>();
builder.Services.AddScoped<IOrderExecutionPublisher, OrderExecutionPublisher>();

builder.Host.AddPalantirMessaging(connectionString, rabbitHost, opts =>
{
    opts.PublishMessage<MarketTickRecordedEvent>().ToRabbitExchange(MessagingConstants.RabbitMqExchanges.MarketData, e =>
    {
        e.ExchangeType = ExchangeType.Fanout;
        e.BindQueue(MessagingConstants.RabbitMqQueues.MarketTicks);
    });
    opts.PublishMessage<OrderBookUpdatedEvent>().ToRabbitExchange(MessagingConstants.RabbitMqExchanges.MarketData, e =>
    {
        e.ExchangeType = ExchangeType.Fanout;
        e.BindQueue(MessagingConstants.RabbitMqQueues.MarketTicks);
    });
    opts.PublishMessage<TradeExecutedEvent>().ToRabbitQueue(MessagingConstants.RabbitMqQueues.SettlementSaga);
}, useEntityFrameworkCore: true);

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseDefaultAsyncApi();

await app.Services.EnsurePostgresSchemaCreatedAsync<MarketTickDbContext>(
    "local_engine",
    app.Services.GetRequiredService<ILogger<Program>>());

app.UsePalantirChassis();
app.MapGrpcService<MatchingEngineGrpcService>();
app.MapGrpcHealthChecksService();

app.Run();
