using Microsoft.Extensions.Hosting;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;

namespace Palantir.Platform.Messaging;

public static class WolverineExtensions
{
    public static IHostBuilder AddPalantirMessaging(
        this IHostBuilder hostBuilder,
        string postgresConnectionString,
        string rabbitMqConnectionString,
        Action<WolverineOptions>? configure = null,
        bool useEntityFrameworkCore = false)
    {
        return hostBuilder.UseWolverine(opts =>
        {
            opts.UseRabbitMq(new Uri(rabbitMqConnectionString))
                .AutoProvision()
                .DisableSystemRequestReplyQueueDeclaration();

            opts.PersistMessagesWithPostgresql(postgresConnectionString, "wolverine");

            if (useEntityFrameworkCore)
            {
                opts.UseEntityFrameworkCoreTransactions();
            }

            opts.Policies.AutoApplyTransactions();
            opts.Policies.UseDurableLocalQueues();
            opts.Policies.UseDurableOutboxOnAllSendingEndpoints();
            opts.Policies.UseDurableInboxOnAllListeners();

            configure?.Invoke(opts);
        });
    }
}
