using Marten;
using Palantir.SharedKernel;

namespace Palantir.OmsService.Repositories;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct = default);
    Task AppendEventsAsync(Order order, CancellationToken ct = default);
}

public class MartenOrderRepository : IOrderRepository
{
    private readonly IDocumentStore _store;

    public MartenOrderRepository(IDocumentStore store)
    {
        _store = store;
    }

    public async Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct = default)
    {
        await using var session = _store.QuerySession();
        var streamEvents = await session.Events.FetchStreamAsync(id.Value, token: ct);
        if (streamEvents == null || streamEvents.Count == 0)
        {
            return null;
        }

        var order = new Order();
        foreach (var evt in streamEvents)
        {
            if (evt.Data is IDomainEvent domainEvent)
            {
                order.Apply(domainEvent);
            }
        }

        order.Version = streamEvents[^1].Version;
        return order;
    }

    public async Task AppendEventsAsync(Order order, CancellationToken ct = default)
    {
        var uncommitted = order.DequeueUncommittedEvents();
        if (uncommitted.Count == 0) return;

        await using var session = _store.LightweightSession();
        session.Events.Append(order.Id.Value, uncommitted.Cast<object>().ToArray());
        await session.SaveChangesAsync(ct);
    }
}
