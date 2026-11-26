using Palantir.Constants;
using Palantir.ExternalGatewayService.Connectors;
using Palantir.Platform;
using Palantir.Platform.Messaging;
using Palantir.ServiceDefaults;
using Palantir.ServiceDefaults.ApiSpecification.AsyncApi;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddDefaultAsyncApi("Palantir External Gateway Service", "1.0.0", typeof(Program));
builder.AddPalantirChassis(typeof(Program).Assembly);

var connectionString = builder.Configuration.GetConnectionString(AspireResourceNames.PalantirDb)
                       ?? "Host=localhost;Database=PalantirDb;Username=postgres;Password=postgres";
var rabbitHost = builder.Configuration.GetConnectionString(AspireResourceNames.RabbitMq)
                 ?? "amqp://guest:guest@localhost:5672";

builder.Services.AddHttpClient<IExchangeVenueConnector, RestWebSocketVenueConnector>(client =>
{
    var baseUri = builder.Configuration["ExternalVenues:MockExchangeUrl"] ?? "http://localhost:5005";
    client.BaseAddress = new Uri(baseUri);
});

builder.Host.AddPalantirMessaging(connectionString, rabbitHost);

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseDefaultAsyncApi();
app.UsePalantirChassis();

app.Run();
