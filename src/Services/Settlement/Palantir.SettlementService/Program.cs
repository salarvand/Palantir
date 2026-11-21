using MassTransit;
using Microsoft.EntityFrameworkCore;
using Palantir.Constants;
using Palantir.Platform.Caching;
using Palantir.Platform.Endpoints;
using Palantir.Platform.Exceptions;
using Palantir.Platform.Security;
using Palantir.ServiceDefaults;
using Palantir.SettlementService.Infrastructure;
using Palantir.SettlementService.Sagas;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var connectionString = builder.Configuration.GetConnectionString(AspireResourceNames.PalantirDb)
                       ?? "Host=localhost;Database=PalantirDb;Username=postgres;Password=postgres";

builder.Services.AddDbContext<SettlementDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

var redisConnectionString = builder.Configuration.GetConnectionString(AspireResourceNames.Redis)
                            ?? "localhost:6379";

builder.Services.AddPalantirCaching(redisConnectionString);
builder.Services.AddPalantirSecurity();
builder.Services.AddPalantirProblemDetails();

builder.Services.AddMassTransit(x =>
{
    x.AddSagaStateMachine<OrderSettlementStateMachine, OrderSettlementState>()
        .EntityFrameworkRepository(r =>
        {
            r.ConcurrencyMode = ConcurrencyMode.Optimistic;
            r.ExistingDbContext<SettlementDbContext>();
            r.UsePostgres();
        });

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitHost = builder.Configuration.GetConnectionString(AspireResourceNames.RabbitMq)
                         ?? "amqp://guest:guest@localhost:5672";
        cfg.Host(new Uri(rabbitHost));
        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

app.MapDefaultEndpoints();
app.UsePalantirExceptionHandler();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SettlementDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.MapGet("/api/v1/settlements/{orderId:guid}", async (Guid orderId, SettlementDbContext db) =>
{
    var state = await db.SettlementStates.FindAsync([orderId]);
    return state != null ? Results.Ok(state) : Results.NotFound();
});

app.Run();
