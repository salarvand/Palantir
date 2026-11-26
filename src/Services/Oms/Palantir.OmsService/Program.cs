using System.Data.Common;
using Marten.Events.Projections;
using Npgsql;
using Orleans.Hosting;
using Palantir.Constants;
using Palantir.LocalEngineService.Grpc;
using Palantir.Oms.Contracts;
using Palantir.OmsService.Execution;
using Palantir.OmsService.Infrastructure;
using Palantir.OmsService.Projections;
using Palantir.OmsService.Repositories;
using Palantir.Platform.Caching;
using Palantir.Platform.Endpoints;
using Palantir.Platform.EventStore;
using Palantir.Platform.Exceptions;
using Palantir.Platform.FeatureFlags;
using Palantir.Platform.Grpc;
using Palantir.Platform.Messaging;
using Palantir.Platform.Security;
using Palantir.ServiceDefaults;
using Wolverine.RabbitMQ;

DbProviderFactories.RegisterFactory("Npgsql", NpgsqlFactory.Instance);

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var connectionString = builder.Configuration.GetConnectionString(AspireResourceNames.PalantirDb)
                       ?? "Host=localhost;Database=PalantirDb;Username=postgres;Password=postgres";
var rabbitHost = builder.Configuration.GetConnectionString(AspireResourceNames.RabbitMq)
                 ?? "amqp://guest:guest@localhost:5672";

await OrleansSchemaInitializer.EnsureInitializedAsync(connectionString);

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

builder.Host.AddPalantirMessaging(connectionString, rabbitHost, opts =>
{
    opts.PublishMessage<InitiateOrderSettlementCommand>().ToRabbitQueue(MessagingConstants.RabbitMqQueues.SettlementSaga);
    opts.PublishMessage<OrderSettlementFailedEvent>().ToRabbitQueue(MessagingConstants.RabbitMqQueues.SettlementSaga);
    opts.PublishMessage<AuditTrailEvent>().ToRabbitQueue(MessagingConstants.RabbitMqQueues.AuditTrail);
});

builder.Services.AddPalantirCaching(connectionString);
builder.Services.AddPalantirSecurity();
builder.Services.AddPalantirProblemDetails();
builder.Services.AddPalantirApiVersioning();
builder.Services.AddPalantirFeatureFlags();

// Marten Event Sourcing & Blotter Projection on PostgreSQL
builder.Services.AddPalantirEventStore(connectionString, opts =>
{
    opts.Projections.Add<OrderBlotterProjection>(ProjectionLifecycle.Async);
});
builder.Services.AddScoped<IOrderRepository, MartenOrderRepository>();

// Resilient gRPC Matching Client (HTTP/2) + Venue Dispatchers
var localEngineUri = new Uri(builder.Configuration.GetConnectionString(AspireResourceNames.LocalEngineService)
                             ?? $"http://{AspireResourceNames.LocalEngineService}");
builder.Services.AddResilientGrpcClient<MatchingEngineGrpc.MatchingEngineGrpcClient>(localEngineUri);
builder.Services.AddScoped<IVenueDispatcher, LocalGrpcVenueDispatcher>();

builder.Services.AddHttpClient<ExternalHttpVenueDispatcher>(client =>
{
    client.BaseAddress = new Uri($"http://{AspireResourceNames.ExternalGatewayService}");
});
builder.Services.AddScoped<IVenueDispatcher>(sp => sp.GetRequiredService<ExternalHttpVenueDispatcher>());
builder.Services.AddScoped<IExecutionRouter, ExecutionRouter>();

builder.Services.AddEndpoints(typeof(Program).Assembly);

var app = builder.Build();

app.MapDefaultEndpoints();
app.UsePalantirExceptionHandler();
app.MapEndpoints();

app.Run();
