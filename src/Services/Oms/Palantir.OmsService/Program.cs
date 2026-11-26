using System.Data.Common;
using Marten.Events.Projections;
using Npgsql;
using Orleans.Configuration;
using Orleans.Hosting;
using Palantir.Constants;
using Palantir.LocalEngineService.Grpc;
using Palantir.OmsService.Execution;
using Palantir.OmsService.Infrastructure;
using Palantir.OmsService.IntegrationEvents.Events;
using Palantir.OmsService.Projections;
using Palantir.OmsService.Repositories;
using Palantir.Platform;
using Palantir.Platform.EventStore;
using Palantir.Platform.Grpc;
using Palantir.Platform.Messaging;
using Palantir.Platform.Security;
using Palantir.ServiceDefaults;
using Palantir.ServiceDefaults.ApiSpecification.AsyncApi;
using Wolverine.RabbitMQ;

DbProviderFactories.RegisterFactory("Npgsql", NpgsqlFactory.Instance);

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddDefaultAsyncApi("Palantir OMS Service", "1.0.0", typeof(Program));
builder.AddPalantirChassis(typeof(Program).Assembly);

var connectionString = builder.Configuration.GetConnectionString(AspireResourceNames.PalantirDb)
                       ?? "Host=localhost;Database=PalantirDb;Username=postgres;Password=postgres";
var rabbitHost = builder.Configuration.GetConnectionString(AspireResourceNames.RabbitMq)
                 ?? "amqp://guest:guest@localhost:5672";

await OrleansSchemaInitializer.EnsureInitializedAsync(connectionString);

builder.Host.UseOrleans(siloBuilder =>
{
    siloBuilder.Configure<ClusterOptions>(options =>
    {
        options.ClusterId = "palantir-oms";
        options.ServiceId = "palantir-oms";
    });

    if (builder.Environment.IsDevelopment())
    {
        siloBuilder.UseLocalhostClustering();
    }
    else
    {
        siloBuilder.UseAdoNetClustering(options =>
        {
            options.Invariant = "Npgsql";
            options.ConnectionString = connectionString;
        });
    }

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

// Marten Event Sourcing & Blotter Projection on PostgreSQL
builder.Services.AddPalantirEventStore(connectionString, opts =>
{
    opts.Projections.Add<OrderBlotterProjection>(ProjectionLifecycle.Async);
});
builder.Services.AddScoped<IOrderRepository, MartenOrderRepository>();

// Resilient gRPC Matching Client (HTTP/2 + Token Exchange) + Venue Dispatchers
var localEngineUri = new Uri(builder.Configuration.GetConnectionString(AspireResourceNames.LocalEngineService)
                             ?? $"http://{AspireResourceNames.LocalEngineService}");
builder.Services.AddResilientGrpcClient<MatchingEngineGrpc.MatchingEngineGrpcClient>(
    localEngineUri,
    AspireResourceNames.LocalEngineService);
builder.Services.AddScoped<IVenueDispatcher, LocalGrpcVenueDispatcher>();

builder.Services.AddHttpClient<ExternalHttpVenueDispatcher>(client =>
{
    client.BaseAddress = new Uri($"http://{AspireResourceNames.ExternalGatewayService}");
}).AddTokenExchangeHandler(AspireResourceNames.ExternalGatewayService);

builder.Services.AddScoped<IVenueDispatcher>(sp => sp.GetRequiredService<ExternalHttpVenueDispatcher>());
builder.Services.AddScoped<IExecutionRouter, ExecutionRouter>();

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseDefaultAsyncApi();
app.UsePalantirChassis();

app.Run();
