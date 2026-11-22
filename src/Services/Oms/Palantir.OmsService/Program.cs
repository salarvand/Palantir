using System.Data.Common;
using MassTransit;
using Npgsql;
using Orleans.Hosting;
using Palantir.Constants;
using Palantir.OmsService.Execution;
using Palantir.Platform.Caching;
using Palantir.Platform.Endpoints;
using Palantir.Platform.Exceptions;
using Palantir.Platform.Security;
using Palantir.ServiceDefaults;

DbProviderFactories.RegisterFactory("Npgsql", NpgsqlFactory.Instance);

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var connectionString = builder.Configuration.GetConnectionString(AspireResourceNames.PalantirDb)
                       ?? "Host=localhost;Database=PalantirDb;Username=postgres;Password=postgres";

builder.Host.UseOrleans(siloBuilder =>
{
    siloBuilder.UseAdoNetClustering(options =>
    {
        options.Invariant = "Npgsql";
        options.ConnectionString = connectionString;
    });

    siloBuilder.AddAdoNetGrainStorage("orders", options =>
    {
        options.Invariant = "Npgsql";
        options.ConnectionString = connectionString;
    });

    siloBuilder.UseAdoNetReminderService(options =>
    {
        options.Invariant = "Npgsql";
        options.ConnectionString = connectionString;
    });
});

var redisConnectionString = builder.Configuration.GetConnectionString(AspireResourceNames.Redis)
                            ?? "localhost:6379";

builder.Services.AddPalantirCaching(redisConnectionString);
builder.Services.AddPalantirSecurity();
builder.Services.AddPalantirProblemDetails();

builder.Services.AddHttpClient<ILocalEngineClient, LocalEngineClient>(client =>
{
    client.BaseAddress = new Uri("http://local-engine-service");
});

builder.Services.AddHttpClient<IExternalGatewayClient, ExternalGatewayClient>(client =>
{
    client.BaseAddress = new Uri("http://external-gateway-service");
});

builder.Services.AddScoped<IExecutionRouter, ExecutionRouter>();

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
