using Microsoft.EntityFrameworkCore;
using Palantir.Constants;
using Palantir.Platform;
using Palantir.Platform.Messaging;
using Palantir.Platform.Persistence;
using Palantir.PortfolioService.Domain;
using Palantir.PortfolioService.Infrastructure;
using Palantir.ServiceDefaults;
using Palantir.ServiceDefaults.ApiSpecification.AsyncApi;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddDefaultAsyncApi("Palantir Portfolio Service", "1.0.0", typeof(Program));
builder.AddPalantirChassis(typeof(Program).Assembly);

var connectionString = builder.Configuration.GetConnectionString(AspireResourceNames.PalantirDb)
                       ?? "Host=localhost;Database=PalantirDb;Username=postgres;Password=postgres";
var rabbitHost = builder.Configuration.GetConnectionString(AspireResourceNames.RabbitMq)
                 ?? "amqp://guest:guest@localhost:5672";

builder.Services.AddDbContext<PortfolioDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsql =>
    {
        npgsql.EnableRetryOnFailure(10, TimeSpan.FromSeconds(5), null);
    });
});

builder.Host.AddPalantirMessaging(connectionString, rabbitHost, opts =>
{
    opts.ListenToRabbitQueue(MessagingConstants.RabbitMqQueues.PortfolioMargin);
}, useEntityFrameworkCore: true);

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseDefaultAsyncApi();

await app.Services.EnsurePostgresSchemaCreatedAsync<PortfolioDbContext>(
    "portfolio",
    app.Services.GetRequiredService<ILogger<Program>>());

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
    if (!await db.Accounts.AnyAsync())
    {
        var demoAccountId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var demoAccount = new Account(demoAccountId, demoAccountId, 100_000m);
        demoAccount.Positions.Add(new Position
        {
            Id = Guid.NewGuid(),
            AccountId = demoAccountId,
            Symbol = "PAL/USD",
            Quantity = 500m,
            AverageEntryPrice = 100.00m,
            CurrentPrice = 105.50m
        });
        db.Accounts.Add(demoAccount);
        await db.SaveChangesAsync();
    }
}

app.UsePalantirChassis();

app.Run();
