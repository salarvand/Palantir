using MassTransit;
using Orleans;
using Palantir.Constants;
using Palantir.Oms.Contracts;
using Palantir.SharedKernel;

namespace Palantir.OmsService.Grains;

[GenerateSerializer]
public class OrderGrainState
{
    public Guid OrderId { get; set; }
    public Guid ClientId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public TradeSide Side { get; set; }
    public OrderType Type { get; set; }
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal CumulativeFilledQuantity { get; set; }
    public decimal AverageFilledPrice { get; set; }
    public OrderStatus Status { get; set; }
    public TimeInForce TimeInForce { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? LastUpdatedAtUtc { get; set; }
    public string? RejectionReason { get; set; }
    public string VenueType { get; set; } = "Local";
}

public class OrderGrain : Grain, IOrderGrain
{
    private OrderGrainState _state = new();
    private readonly IPublishEndpoint _publishEndpoint;

    public OrderGrain(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public async Task<OrderStateSnapshot> SubmitOrderAsync(SubmitOrderRequest request)
    {
        _state.OrderId = request.OrderId;
        _state.ClientId = request.ClientId;
        _state.Symbol = request.Symbol;
        _state.Side = request.Side;
        _state.Type = request.Type;
        _state.Quantity = request.Quantity;
        _state.Price = request.Price;
        _state.TimeInForce = request.TimeInForce;
        _state.VenueType = request.VenueType;
        _state.Status = OrderStatus.Submitted;
        _state.CreatedAtUtc = DateTimeOffset.UtcNow;
        _state.LastUpdatedAtUtc = DateTimeOffset.UtcNow;

        var clientSession = GrainFactory.GetGrain<IClientSessionGrain>(request.ClientId.ToString("N"));
        await clientSession.RegisterOrderAsync(request.OrderId);

        return ToSnapshot();
    }

    public async Task<OrderStateSnapshot> CancelOrderAsync(string reason)
    {
        if (_state.Status is OrderStatus.Filled or OrderStatus.Cancelled or OrderStatus.Rejected)
        {
            return ToSnapshot();
        }

        _state.Status = OrderStatus.Cancelled;
        _state.RejectionReason = reason;
        _state.LastUpdatedAtUtc = DateTimeOffset.UtcNow;

        var clientSession = GrainFactory.GetGrain<IClientSessionGrain>(_state.ClientId.ToString("N"));
        await clientSession.UnregisterOrderAsync(_state.OrderId);

        return ToSnapshot();
    }

    public async Task<OrderStateSnapshot> ApplyExecutionReportAsync(decimal fillQuantity, decimal fillPrice, string tradeId)
    {
        var totalCost = (_state.CumulativeFilledQuantity * _state.AverageFilledPrice) + (fillQuantity * fillPrice);
        _state.CumulativeFilledQuantity += fillQuantity;
        _state.AverageFilledPrice = totalCost / _state.CumulativeFilledQuantity;

        if (_state.CumulativeFilledQuantity >= _state.Quantity)
        {
            _state.Status = OrderStatus.Filled;
            var clientSession = GrainFactory.GetGrain<IClientSessionGrain>(_state.ClientId.ToString("N"));
            await clientSession.UnregisterOrderAsync(_state.OrderId);
        }
        else
        {
            _state.Status = OrderStatus.PartiallyFilled;
        }

        _state.LastUpdatedAtUtc = DateTimeOffset.UtcNow;
        return ToSnapshot();
    }

    public Task<OrderStateSnapshot> GetSnapshotAsync() => Task.FromResult(ToSnapshot());

    private OrderStateSnapshot ToSnapshot() => new(
        _state.OrderId,
        _state.ClientId,
        _state.Symbol,
        _state.Side,
        _state.Type,
        _state.Quantity,
        _state.Price,
        _state.CumulativeFilledQuantity,
        _state.AverageFilledPrice,
        _state.Status,
        _state.TimeInForce,
        _state.CreatedAtUtc,
        _state.LastUpdatedAtUtc,
        _state.RejectionReason
    );
}

public class ClientSessionGrain : Grain, IClientSessionGrain
{
    private readonly HashSet<Guid> _activeOrders = [];

    public Task RegisterOrderAsync(Guid orderId)
    {
        _activeOrders.Add(orderId);
        return Task.CompletedTask;
    }

    public Task UnregisterOrderAsync(Guid orderId)
    {
        _activeOrders.Remove(orderId);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Guid>> GetActiveOrdersAsync() =>
        Task.FromResult<IReadOnlyList<Guid>>(_activeOrders.ToList());
}
