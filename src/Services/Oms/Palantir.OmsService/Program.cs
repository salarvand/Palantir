using System.Data.Common;
using Marten.Events.Projections;
using MassTransit;
using Npgsql;
using Orleans.Hosting;
using Palantir.Constants;
using Palantir.LocalEngineService.Grpc;
using Palantir.OmsService.Execution;
using Palantir.OmsService.Projections;
using Palantir.OmsService.Repositories;
using Palantir.Platform.Caching;
using Palantir.Platform.CQRS;
using Palantir.Platform.Endpoints;
using Palantir.Platform.EventStore;
using Palantir.Platform.FeatureFlags;
using Palantir.Platform.Grpc;
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
builder.Services.AddPalantirApiVersioning();
builder.Services.AddPalantirFeatureFlags();

// CQRS Pipeline Behaviors
builder.Services.AddPalantirCqrs(typeof(Program).Assembly);

// Marten Event Sourcing & Blotter Projection
builder.Services.AddPalantirEventStore(connectionString, opts =>
{
    opts.Projections.Add<OrderBlotterProjection>(ProjectionLifecycle.Async);
});
builder.Services.AddScoped<IOrderRepository, MartenOrderRepository>();

// Resilient gRPC Matching Client (HTTP/2)
var localEngineUri = new Uri(builder.Configuration.GetConnectionString(AspireResourceNames.LocalEngineService)
                             ?? "http://local-engine-service");
builder.Services.AddResilientGrpcClient<MatchingEngineGrpc.MatchingEngineGrpcClient>(localEngineUri);

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
