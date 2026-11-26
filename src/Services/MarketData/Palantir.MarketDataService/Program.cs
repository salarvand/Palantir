using Palantir.Constants;
using Palantir.MarketDataService.Hubs;
using Palantir.Platform;
using Palantir.Platform.Messaging;
using Palantir.ServiceDefaults;
using Palantir.ServiceDefaults.ApiSpecification.AsyncApi;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddDefaultAsyncApi("Palantir Market Data Service", "1.0.0", typeof(Program));
builder.AddPalantirChassis(typeof(Program).Assembly);
builder.Services.AddSignalR();

var connectionString = builder.Configuration.GetConnectionString(AspireResourceNames.PalantirDb)
                       ?? "Host=localhost;Database=PalantirDb;Username=postgres;Password=postgres";
var rabbitHost = builder.Configuration.GetConnectionString(AspireResourceNames.RabbitMq)
                 ?? "amqp://guest:guest@localhost:5672";

builder.Host.AddPalantirMessaging(connectionString, rabbitHost, opts =>
{
    opts.ListenToRabbitQueue(MessagingConstants.RabbitMqQueues.MarketTicks);
});

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseDefaultAsyncApi();
app.UsePalantirChassis();
app.MapHub<MarketDataHub>("/hubs/marketdata");

app.Run();
