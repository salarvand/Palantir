using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Hosting;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.ErrorHandling;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;

namespace Palantir.Platform.Messaging;

public static class WolverineExtensions
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static IHostBuilder AddPalantirMessaging(
        this IHostBuilder hostBuilder,
        string postgresConnectionString,
        string rabbitMqConnectionString,
        Action<WolverineOptions>? configure = null,
        bool useEntityFrameworkCore = false,
        Assembly? serviceAssembly = null)
    {
        var resolvedAssembly = serviceAssembly
            ?? ResolveServiceAssembly(Assembly.GetCallingAssembly(), Assembly.GetEntryAssembly());

        return hostBuilder.UseWolverine(opts =>
        {
            if (resolvedAssembly is not null)
            {
                opts.ApplicationAssembly = resolvedAssembly;
                opts.Discovery.IncludeAssembly(resolvedAssembly);
            }

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

            opts.AddPalantirDeadLetterHandling();

            configure?.Invoke(opts);
        });
    }

    private static Assembly? ResolveServiceAssembly(Assembly callingAssembly, Assembly? entryAssembly) =>
        callingAssembly != typeof(WolverineExtensions).Assembly
            ? callingAssembly
            : entryAssembly;
}
