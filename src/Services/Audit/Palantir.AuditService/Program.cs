using Elastic.Clients.Elasticsearch;
using MassTransit;
using Palantir.AuditService.Domain;
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

var esUrl = builder.Configuration.GetConnectionString(AspireResourceNames.Elasticsearch)
            ?? builder.Configuration["Elasticsearch:Url"]
            ?? builder.Configuration["Elasticsearch__Url"]
            ?? "http://localhost:9200";

var esSettings = new ElasticsearchClientSettings(new Uri(esUrl))
    .DefaultIndex("audit-records-v1");

builder.Services.AddSingleton(new ElasticsearchClient(esSettings));
builder.Services.AddHostedService<AuditIndexInitializer>();

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

public class AuditIndexInitializer(ElasticsearchClient esClient, ILogger<AuditIndexInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var exists = await esClient.Indices.ExistsAsync("audit-records-v1", cancellationToken);
            if (!exists.Exists)
            {
                await esClient.Indices.CreateAsync("audit-records-v1", cancellationToken);
                logger.LogInformation("Elasticsearch index 'audit-records-v1' initialized successfully.");
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not initialize Elasticsearch index 'audit-records-v1'. It may be created on first write.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
