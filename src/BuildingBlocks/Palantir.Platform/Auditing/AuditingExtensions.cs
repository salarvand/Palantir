using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Palantir.Platform.Auditing.Sinks;
using Palantir.Platform.Observability;

namespace Palantir.Platform.Auditing;

public sealed class AuditingOptions
{
    public int ChannelCapacity { get; set; } = 10_000;
    public bool EnablePostgresSink { get; set; } = true;
    public bool EnableOtlpSink { get; set; } = true;
    public bool EnableStructuredLogSink { get; set; } = true;
    public bool EnableRabbitMqSink { get; set; }
}

public static class AuditingExtensions
{
    public static IServiceCollection AddPalantirAuditing(
        this IServiceCollection services,
        string connectionString,
        Action<AuditingOptions>? configure = null)
    {
        var options = new AuditingOptions();
        configure?.Invoke(options);

        var channel = Channel.CreateBounded<AuditEntry>(
            new BoundedChannelOptions(options.ChannelCapacity)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true
            });

        services.TryAddSingleton(channel);
        services.TryAddSingleton<ITraceContextAccessor, ActivityTraceContextAccessor>();
        services.TryAddSingleton<IAuditEmitter, ChannelAuditEmitter>();

        if (options.EnablePostgresSink)
        {
            services.AddSingleton<IAuditSink>(new PostgresAuditSink(connectionString));
        }

        if (options.EnableOtlpSink)
        {
            services.AddSingleton<IAuditSink, OtlpAuditSink>();
        }

        if (options.EnableStructuredLogSink)
        {
            services.AddSingleton<IAuditSink, StructuredLogAuditSink>();
        }

        if (options.EnableRabbitMqSink)
        {
            services.AddSingleton<IAuditSink, RabbitMqAuditSink>();
        }

        services.AddHostedService<AuditPipelineService>();

        return services;
    }
}
