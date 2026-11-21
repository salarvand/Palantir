using MassTransit;
using Palantir.Constants;
using Palantir.ExternalGatewayService.Connectors;
using Palantir.Platform.Caching;
using Palantir.Platform.Endpoints;
using Palantir.Platform.Exceptions;
using Palantir.Platform.Security;
using Palantir.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var redisConnectionString = builder.Configuration.GetConnectionString(AspireResourceNames.Redis)
                            ?? "localhost:6379";

builder.Services.AddPalantirCaching(redisConnectionString);
builder.Services.AddPalantirSecurity();
builder.Services.AddPalantirProblemDetails();

builder.Services.AddHttpClient<IExchangeVenueConnector, RestWebSocketVenueConnector>(client =>
{
    var baseUri = builder.Configuration["ExternalVenues:MockExchangeUrl"] ?? "http://localhost:5005";
    client.BaseAddress = new Uri(baseUri);
});

builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitHost = builder.Configuration.GetConnectionString(AspireResourceNames.RabbitMq)
                         ?? "amqp://guest:guest@localhost:5672";
        cfg.Host(new Uri(rabbitHost));
        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddEndpoints(typeof(Program).Assembly);

var app = builder.Build();

app.MapDefaultEndpoints();
app.UsePalantirExceptionHandler();

app.MapEndpoints();

app.Run();
