using MassTransit;
using Orleans;
using Orleans.Runtime;
using Palantir.Constants;
using Palantir.Oms.Contracts;
using Palantir.OmsService.Execution;
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
    private readonly IPersistentState<OrderGrainState> _state;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IExecutionRouter _executionRouter;

    public OrderGrain(
        [PersistentState("orders", "orders")] IPersistentState<OrderGrainState> state,
        IPublishEndpoint publishEndpoint,
        IExecutionRouter executionRouter)
    {
        _state = state;
        _publishEndpoint = publishEndpoint;
        _executionRouter = executionRouter;
    }

    public async Task<OrderStateSnapshot> SubmitOrderAsync(SubmitOrderRequest request)
    {
        if (IsTerminal(_state.State.Status))
        {
            return ToSnapshot();
        }

        _state.State.OrderId = request.OrderId;
        _state.State.ClientId = request.ClientId;
        _state.State.Symbol = request.Symbol;
        _state.State.Side = request.Side;
        _state.State.Type = request.Type;
        _state.State.Quantity = request.Quantity;
        _state.State.Price = request.Price;
        _state.State.TimeInForce = request.TimeInForce;
        _state.State.VenueType = request.VenueType;
        _state.State.Status = OrderStatus.Submitted;
        _state.State.CreatedAtUtc = DateTimeOffset.UtcNow;
        _state.State.LastUpdatedAtUtc = DateTimeOffset.UtcNow;

        var clientSession = GrainFactory.GetGrain<IClientSessionGrain>(request.ClientId.ToString("N"));
        await clientSession.RegisterOrderAsync(request.OrderId);

        if (request.TimeInForce == TimeInForce.GoodTilTime && request.ExpiryDuration.HasValue)
        {
            var expiryGrain = GrainFactory.GetGrain<IOrderExpiryGrain>(request.OrderId);
            await expiryGrain.TrackOrderExpiryAsync(request.OrderId, request.ExpiryDuration.Value);
        }

        await _state.WriteStateAsync();

        var dispatchResult = await _executionRouter.DispatchOrderAsync(request);
        if (dispatchResult.IsSuccess)
        {
            _state.State.Status = OrderStatus.Accepted;
        }
        else
        {
            _state.State.Status = OrderStatus.Rejected;
            _state.State.RejectionReason = dispatchResult.ErrorMessage ?? "Execution venue dispatch failed";
            await clientSession.UnregisterOrderAsync(request.OrderId);
        }

        _state.State.LastUpdatedAtUtc = DateTimeOffset.UtcNow;
        await _state.WriteStateAsync();

        return ToSnapshot();
    }

    public async Task<OrderStateSnapshot> CancelOrderAsync(string reason)
    {
        if (IsTerminal(_state.State.Status))
        {
            return ToSnapshot();
        }

        _state.State.Status = OrderStatus.Cancelled;
        _state.State.RejectionReason = reason;
        _state.State.LastUpdatedAtUtc = DateTimeOffset.UtcNow;

        var clientSession = GrainFactory.GetGrain<IClientSessionGrain>(_state.State.ClientId.ToString("N"));
        await clientSession.UnregisterOrderAsync(_state.State.OrderId);

        if (_state.State.TimeInForce == TimeInForce.GoodTilTime)
        {
            var expiryGrain = GrainFactory.GetGrain<IOrderExpiryGrain>(_state.State.OrderId);
            await expiryGrain.CancelOrderExpiryAsync();
        }

        await _state.WriteStateAsync();

        return ToSnapshot();
    }

    public async Task<OrderStateSnapshot> ExpireOrderAsync()
    {
        if (IsTerminal(_state.State.Status))
        {
            return ToSnapshot();
        }

        _state.State.Status = OrderStatus.Expired;
        _state.State.RejectionReason = "Order expired (Good-Til-Time elapsed)";
        _state.State.LastUpdatedAtUtc = DateTimeOffset.UtcNow;

        var clientSession = GrainFactory.GetGrain<IClientSessionGrain>(_state.State.ClientId.ToString("N"));
        await clientSession.UnregisterOrderAsync(_state.State.OrderId);

        await _state.WriteStateAsync();

        return ToSnapshot();
    }

    public async Task<OrderStateSnapshot> ApplyExecutionReportAsync(decimal fillQuantity, decimal fillPrice, string tradeId)
    {
        if (IsTerminal(_state.State.Status))
        {
            return ToSnapshot();
        }

        var totalCost = (_state.State.CumulativeFilledQuantity * _state.State.AverageFilledPrice) + (fillQuantity * fillPrice);
        _state.State.CumulativeFilledQuantity += fillQuantity;
        _state.State.AverageFilledPrice = _state.State.CumulativeFilledQuantity > 0 
            ? totalCost / _state.State.CumulativeFilledQuantity 
            : 0m;

        if (_state.State.CumulativeFilledQuantity >= _state.State.Quantity)
        {
            _state.State.Status = OrderStatus.Filled;
            var clientSession = GrainFactory.GetGrain<IClientSessionGrain>(_state.State.ClientId.ToString("N"));
            await clientSession.UnregisterOrderAsync(_state.State.OrderId);

            if (_state.State.TimeInForce == TimeInForce.GoodTilTime)
            {
                var expiryGrain = GrainFactory.GetGrain<IOrderExpiryGrain>(_state.State.OrderId);
                await expiryGrain.CancelOrderExpiryAsync();
            }
        }
        else
        {
            _state.State.Status = OrderStatus.PartiallyFilled;
        }

        _state.State.LastUpdatedAtUtc = DateTimeOffset.UtcNow;
        await _state.WriteStateAsync();

        return ToSnapshot();
    }

    public Task<OrderStateSnapshot> GetSnapshotAsync() => Task.FromResult(ToSnapshot());

    private static bool IsTerminal(OrderStatus status) =>
        status is OrderStatus.Filled or OrderStatus.Cancelled or OrderStatus.Rejected or OrderStatus.Expired;

    private OrderStateSnapshot ToSnapshot() => new(
        _state.State.OrderId,
        _state.State.ClientId,
        _state.State.Symbol,
        _state.State.Side,
        _state.State.Type,
        _state.State.Quantity,
        _state.State.Price,
        _state.State.CumulativeFilledQuantity,
        _state.State.AverageFilledPrice,
        _state.State.Status,
        _state.State.TimeInForce,
        _state.State.CreatedAtUtc,
        _state.State.LastUpdatedAtUtc,
        _state.State.RejectionReason
    );
}

public class OrderExpiryGrain : Grain, IOrderExpiryGrain, IRemindable
{
    private const string ExpiryReminderName = "OrderExpiryReminder";
    private IGrainReminder? _reminder;
    private Guid _orderId;

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _orderId = this.GetPrimaryKey();
        return base.OnActivateAsync(cancellationToken);
    }

    public async Task TrackOrderExpiryAsync(Guid orderId, TimeSpan duration)
    {
        _orderId = orderId;
        _reminder = await this.RegisterOrUpdateReminder(
            ExpiryReminderName,
            dueTime: duration,
            period: TimeSpan.FromMinutes(1));
    }

    public async Task CancelOrderExpiryAsync()
    {
        var reminder = _reminder ?? await this.GetReminder(ExpiryReminderName);
        if (reminder != null)
        {
            await this.UnregisterReminder(reminder);
            _reminder = null;
        }
    }

    public async Task ReceiveReminder(string reminderName, TickStatus status)
    {
        if (reminderName == ExpiryReminderName)
        {
            var orderGrain = GrainFactory.GetGrain<IOrderGrain>(_orderId);
            await orderGrain.ExpireOrderAsync();

            var reminder = _reminder ?? await this.GetReminder(ExpiryReminderName);
            if (reminder != null)
            {
                await this.UnregisterReminder(reminder);
                _reminder = null;
            }
        }
    }
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
