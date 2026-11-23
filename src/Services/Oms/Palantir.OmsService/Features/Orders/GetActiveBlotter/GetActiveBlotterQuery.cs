using Palantir.OmsService.Projections;
using Palantir.Platform.CQRS;

namespace Palantir.OmsService.Features.Orders.GetActiveBlotter;

public record GetActiveBlotterQuery(
    Guid? ClientId = null,
    string? Symbol = null,
    int Limit = 50
) : IQuery<IReadOnlyList<OrderBlotterView>>;
