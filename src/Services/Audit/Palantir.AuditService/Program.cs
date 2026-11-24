using MassTransit;
using Meilisearch;
using Palantir.AuditService.Domain;
using Palantir.AuditService.Infrastructure;
using Palantir.Constants;
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

var meiliUrl = builder.Configuration.GetConnectionString(AspireResourceNames.Meilisearch)
               ?? builder.Configuration["Meilisearch:Url"]
               ?? builder.Configuration["Meilisearch__Url"]
               ?? "http://localhost:7700";
var meiliApiKey = builder.Configuration["Meilisearch:ApiKey"]
                  ?? builder.Configuration["Meilisearch__ApiKey"]
                  ?? "palantir-master-key-dev-12345678";

builder.Services.AddSingleton(new MeilisearchClient(meiliUrl, meiliApiKey));
builder.Services.AddSingleton<IAuditLogRepository, MeilisearchAuditLogRepository>();
builder.Services.AddHostedService<MeilisearchIndexInitializer>();

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
