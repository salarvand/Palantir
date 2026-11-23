using Palantir.OmsService.Repositories;
using Palantir.Platform.CQRS;
using Palantir.SharedKernel;
using ZiggyCreatures.Caching.Fusion;

namespace Palantir.OmsService.Features.Orders.GetOrderById;

public class GetOrderByIdHandler : IQueryHandler<GetOrderByIdQuery, OrderDto?>
{
    private readonly IOrderRepository _repository;
    private readonly IFusionCache _cache;

    public GetOrderByIdHandler(IOrderRepository repository, IFusionCache cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<OrderDto?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"order:{request.OrderId:N}";

        return await _cache.GetOrSetAsync(
            cacheKey,
            async ct =>
            {
                var order = await _repository.GetByIdAsync(OrderId.From(request.OrderId), ct);
                if (order == null) return null;

                return new OrderDto(
                    order.Id.Value,
                    order.ClientId.Value,
                    order.Symbol.Value,
                    order.Side.ToString(),
                    order.Type.ToString(),
                    order.Quantity.Value,
                    order.Price.Value,
                    order.Status.ToString(),
                    order.CumulativeFilledQuantity.Value,
                    order.AverageFilledPrice.Value,
                    order.CreatedAtUtc,
                    order.UpdatedAtUtc
                );
            },
            options => options.SetDuration(TimeSpan.FromMinutes(2)),
            token: cancellationToken
        );
    }
}
