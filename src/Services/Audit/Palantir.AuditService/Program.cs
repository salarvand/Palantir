using Microsoft.EntityFrameworkCore;
using Palantir.AuditService.Infrastructure;
using Palantir.Constants;
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

var connectionString = builder.Configuration.GetConnectionString(AspireResourceNames.PalantirDb)
                       ?? "Host=localhost;Database=PalantirDb;Username=postgres;Password=postgres";
var rabbitHost = builder.Configuration.GetConnectionString(AspireResourceNames.RabbitMq)
                 ?? "amqp://guest:guest@localhost:5672";

builder.Services.AddDbContext<AuditDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsql =>
    {
        npgsql.EnableRetryOnFailure(10, TimeSpan.FromSeconds(5), null);
    });
});

builder.Services.AddPalantirCaching(connectionString);
builder.Services.AddPalantirSecurity();
builder.Services.AddPalantirProblemDetails();
builder.Services.AddScoped<IAuditLogRepository, PostgresAuditLogRepository>();

builder.Host.AddPalantirMessaging(connectionString, rabbitHost, opts =>
{
    opts.ListenToRabbitQueue(MessagingConstants.RabbitMqQueues.AuditTrail);
}, useEntityFrameworkCore: true);

builder.Services.AddEndpoints(typeof(Program).Assembly);

var app = builder.Build();

app.MapDefaultEndpoints();
app.UsePalantirExceptionHandler();

await app.Services.EnsurePostgresSchemaCreatedAsync<AuditDbContext>(
    "audit",
    app.Services.GetRequiredService<ILogger<Program>>());

app.MapEndpoints();

app.Run();
