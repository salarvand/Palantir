using MassTransit;
using Microsoft.EntityFrameworkCore;
using Palantir.Constants;
using Palantir.LocalEngineService.Timescale;
using Palantir.Platform.Caching;
using Palantir.Platform.Endpoints;
using Palantir.Platform.Exceptions;
using Palantir.Platform.Security;
using Palantir.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var timescaleConn = builder.Configuration.GetConnectionString(AspireResourceNames.TimescaleDb)
                    ?? builder.Configuration.GetConnectionString(AspireResourceNames.PalantirDb)
                    ?? "Host=localhost;Database=TimescaleDb;Username=postgres;Password=postgres";

builder.Services.AddDbContext<TimescaleDbContext>(options =>
{
    options.UseNpgsql(timescaleConn);
});

var redisConnectionString = builder.Configuration.GetConnectionString(AspireResourceNames.Redis)
                            ?? "localhost:6379";

builder.Services.AddPalantirCaching(redisConnectionString);
builder.Services.AddPalantirSecurity();
builder.Services.AddPalantirProblemDetails();

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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TimescaleDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.MapEndpoints();

app.Run();
