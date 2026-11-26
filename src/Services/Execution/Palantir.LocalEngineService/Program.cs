using Microsoft.EntityFrameworkCore;
using Palantir.Constants;
using Palantir.LocalEngineService.Grpc;
using Palantir.LocalEngineService.Matching;
using Palantir.LocalEngineService.Persistence;
using Palantir.Oms.Contracts;
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
builder.Services.AddGrpc();

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

builder.Services.AddPalantirCaching(connectionString);
builder.Services.AddPalantirSecurity();
builder.Services.AddPalantirProblemDetails();

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

builder.Services.AddEndpoints(typeof(Program).Assembly);

var app = builder.Build();

app.MapDefaultEndpoints();
app.UsePalantirExceptionHandler();

await app.Services.EnsurePostgresSchemaCreatedAsync<MarketTickDbContext>(
    "local_engine",
    app.Services.GetRequiredService<ILogger<Program>>());

app.MapEndpoints();
app.MapGrpcService<MatchingEngineGrpcService>();

app.Run();
