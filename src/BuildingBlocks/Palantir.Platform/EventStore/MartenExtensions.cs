using Marten;
using Marten.Events.Daemon.Resiliency;
using Microsoft.Extensions.DependencyInjection;
using Weasel.Core;

namespace Palantir.Platform.EventStore;

public static class MartenExtensions
{
    public static IServiceCollection AddPalantirEventStore(
        this IServiceCollection services,
        string connectionString,
        Action<StoreOptions>? configure = null)
    {
        services.AddMarten(opts =>
        {
            opts.Connection(connectionString);
            opts.DatabaseSchemaName = "event_store";
            opts.AutoCreateSchemaObjects = AutoCreate.All;

            opts.Events.MetadataConfig.CorrelationIdEnabled = true;
            opts.Events.MetadataConfig.CausationIdEnabled = true;
            opts.Events.MetadataConfig.HeadersEnabled = true;

            configure?.Invoke(opts);
        })
        .AddAsyncDaemon(DaemonMode.Solo)
        .UseLightweightSessions();

        return services;
    }
}
