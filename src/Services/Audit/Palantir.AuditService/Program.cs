using Microsoft.EntityFrameworkCore;
using Palantir.AuditService.Infrastructure;
using Palantir.Constants;
using Palantir.Platform;
using Palantir.Platform.Messaging;
using Palantir.Platform.Persistence;
using Palantir.ServiceDefaults;
using Palantir.ServiceDefaults.ApiSpecification.AsyncApi;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddDefaultAsyncApi("Palantir Audit Service", "1.0.0", typeof(Program));
builder.AddPalantirChassis(typeof(Program).Assembly);

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

builder.Services.AddScoped<IAuditLogRepository, PostgresAuditLogRepository>();

builder.Host.AddPalantirMessaging(connectionString, rabbitHost, opts =>
{
    opts.ListenToRabbitQueue(MessagingConstants.RabbitMqQueues.AuditTrail);
}, useEntityFrameworkCore: true);

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseDefaultAsyncApi();

await app.Services.EnsurePostgresSchemaCreatedAsync<AuditDbContext>(
    "audit",
    app.Services.GetRequiredService<ILogger<Program>>());

using (var scope = app.Services.CreateScope())
{
    var auditDb = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
    await auditDb.Database.ExecuteSqlRawAsync(
        """
        ALTER TABLE audit.audit_records ADD COLUMN IF NOT EXISTS "TraceId" character varying(32);
        CREATE INDEX IF NOT EXISTS ix_audit_records_trace_id ON audit.audit_records ("TraceId");
        """);
}

app.UsePalantirChassis();

app.Run();
