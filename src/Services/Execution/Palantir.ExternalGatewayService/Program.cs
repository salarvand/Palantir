using Palantir.Constants;
using Palantir.ExternalGatewayService.Connectors;
using Palantir.Platform.Caching;
using Palantir.Platform.Endpoints;
using Palantir.Platform.Exceptions;
using Palantir.Platform.Messaging;
using Palantir.Platform.Security;
using Palantir.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var connectionString = builder.Configuration.GetConnectionString(AspireResourceNames.PalantirDb)
                       ?? "Host=localhost;Database=PalantirDb;Username=postgres;Password=postgres";
var rabbitHost = builder.Configuration.GetConnectionString(AspireResourceNames.RabbitMq)
                 ?? "amqp://guest:guest@localhost:5672";

builder.Services.AddPalantirCaching(connectionString);
builder.Services.AddPalantirSecurity();
builder.Services.AddPalantirProblemDetails();

builder.Services.AddHttpClient<IExchangeVenueConnector, RestWebSocketVenueConnector>(client =>
{
    var baseUri = builder.Configuration["ExternalVenues:MockExchangeUrl"] ?? "http://localhost:5005";
    client.BaseAddress = new Uri(baseUri);
});

builder.Host.AddPalantirMessaging(connectionString, rabbitHost);

builder.Services.AddEndpoints(typeof(Program).Assembly);

var app = builder.Build();

app.MapDefaultEndpoints();
app.UsePalantirExceptionHandler();
app.MapEndpoints();

app.Run();
