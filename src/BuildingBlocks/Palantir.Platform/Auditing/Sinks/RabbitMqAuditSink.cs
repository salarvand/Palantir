using Microsoft.Extensions.DependencyInjection;
using Wolverine;

namespace Palantir.Platform.Auditing.Sinks;

/// <summary>
/// Publishes batched audit entries onto the Wolverine/RabbitMQ bus using isolated DI scopes.
/// </summary>
public sealed class RabbitMqAuditSink(IServiceScopeFactory scopeFactory) : IAuditSink
{
    public string Name => "RabbitMQ";

    public async Task FlushAsync(IReadOnlyList<AuditEntry> batch, CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetService<IMessageBus>();
        if (bus is null)
        {
            return;
        }

        foreach (var entry in batch)
        {
            await bus.PublishAsync(entry);
        }
    }
}
