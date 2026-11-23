using Marten;
using Palantir.OmsService.Projections;
using Palantir.Platform.CQRS;

namespace Palantir.OmsService.Features.Orders.GetActiveBlotter;

public class GetActiveBlotterHandler : IQueryHandler<GetActiveBlotterQuery, IReadOnlyList<OrderBlotterView>>
{
    private readonly IQuerySession _session;

    public GetActiveBlotterHandler(IQuerySession session)
    {
        _session = session;
    }

    public async Task<IReadOnlyList<OrderBlotterView>> Handle(GetActiveBlotterQuery request, CancellationToken cancellationToken)
    {
        var query = _session.Query<OrderBlotterView>();

        if (request.ClientId.HasValue)
        {
            query = (Marten.Linq.IMartenQueryable<OrderBlotterView>)query.Where(x => x.ClientId == request.ClientId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Symbol))
        {
            var sym = request.Symbol.Trim().ToUpperInvariant();
            query = (Marten.Linq.IMartenQueryable<OrderBlotterView>)query.Where(x => x.Symbol == sym);
        }

        var results = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(Math.Clamp(request.Limit, 1, 200))
            .ToListAsync(cancellationToken);

        return results;
    }
}
