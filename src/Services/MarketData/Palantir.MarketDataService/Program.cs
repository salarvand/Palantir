using Palantir.Constants;
using Palantir.MarketDataService.Hubs;
using Palantir.Platform.Caching;
using Palantir.Platform.Endpoints;
using Palantir.Platform.Exceptions;
using Palantir.Platform.Messaging;
using Palantir.Platform.Security;
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
builder.Services.AddSignalR();

builder.Host.AddPalantirMessaging(connectionString, rabbitHost, opts =>
{
    opts.ListenToRabbitQueue(MessagingConstants.RabbitMqQueues.MarketTicks);
});

builder.Services.AddEndpoints(typeof(Program).Assembly);

var app = builder.Build();

app.MapDefaultEndpoints();
app.UsePalantirExceptionHandler();
app.MapHub<MarketDataHub>("/hubs/marketdata");
app.MapEndpoints();

app.Run();
