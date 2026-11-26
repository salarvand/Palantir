using System.Text.Json;
using Orleans;
using Orleans.Runtime;
using Palantir.OmsService.Execution;
using Palantir.OmsService.IntegrationEvents.Events;
using Palantir.OmsService.Repositories;
using Palantir.SharedKernel;
using Wolverine;

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
    private readonly IOrderRepository _orderRepository;
    private readonly IMessageBus _messageBus;
    private readonly IExecutionRouter _executionRouter;

    public OrderGrain(
        [PersistentState("orders", "orders")] IPersistentState<OrderGrainState> state,
        IOrderRepository orderRepository,
        IMessageBus messageBus,
        IExecutionRouter executionRouter)
    {
        _state = state;
        _orderRepository = orderRepository;
        _messageBus = messageBus;
        _executionRouter = executionRouter;
    }

    public async Task<OrderStateSnapshot> SubmitOrderAsync(SubmitOrderRequest request)
    {
        if (IsTerminal(_state.State.Status) || _state.State.OrderId != Guid.Empty)
        {
            return ToSnapshot();
        }

        var order = Order.Create(
            OrderId.From(request.OrderId),
            ClientId.From(request.ClientId),
            SymbolId.From(request.Symbol),
            request.Side,
            request.Type,
            new Quantity(request.Quantity),
            new Price(request.Price),
            request.TimeInForce);

        var initialMargin = request.Quantity * (request.Price > 0 ? request.Price : 100m) * 0.1m;
        order.ApproveRisk(initialMargin);

        ApplyDomainStateToGrain(request, order);

        var clientSession = GrainFactory.GetGrain<IClientSessionGrain>(request.ClientId.ToString("N"));
        await clientSession.RegisterOrderAsync(request.OrderId);

        if (request.TimeInForce == TimeInForce.GoodTilTime && request.ExpiryDuration.HasValue)
        {
            var expiryGrain = GrainFactory.GetGrain<IOrderExpiryGrain>(request.OrderId);
            await expiryGrain.TrackOrderExpiryAsync(request.OrderId, request.ExpiryDuration.Value);
        }

        await _orderRepository.AppendEventsAsync(order);
        await _state.WriteStateAsync();

        var dispatchResult = await _executionRouter.DispatchOrderAsync(request);
        if (dispatchResult.IsSuccess)
        {
            if (dispatchResult.FilledQuantity > 0 && dispatchResult.TradeId.HasValue)
            {
                order.ApplyExecution(
                    TradeId.From(dispatchResult.TradeId.Value),
                    new Quantity(dispatchResult.FilledQuantity),
                    new Price(dispatchResult.AveragePrice),
                    request.VenueType);
                _state.State.CumulativeFilledQuantity = order.CumulativeFilledQuantity.Value;
                _state.State.AverageFilledPrice = order.AverageFilledPrice.Value;
                _state.State.Status = order.Status;
                if (order.IsTerminal)
                {
                    await UnregisterSessionAndExpiryAsync();
                }
            }
            else
            {
                _state.State.Status = OrderStatus.Accepted;
            }

            await _messageBus.PublishAsync(new InitiateOrderSettlementCommand(
                request.OrderId,
                request.ClientId,
                request.Symbol,
                request.Quantity,
                request.Price));
        }
        else
        {
            var reason = dispatchResult.ErrorMessage ?? "Execution venue dispatch failed";
            order.Reject(reason);
            _state.State.Status = OrderStatus.Rejected;
            _state.State.RejectionReason = reason;
            await clientSession.UnregisterOrderAsync(request.OrderId);
        }

        _state.State.LastUpdatedAtUtc = DateTimeOffset.UtcNow;
        await _orderRepository.AppendEventsAsync(order);
        await _state.WriteStateAsync();
        await PublishAuditAsync("OrderSubmitted", null, request.Price, request.Quantity);

        return ToSnapshot();
    }

    public async Task<OrderStateSnapshot> CancelOrderAsync(string reason)
    {
        if (IsTerminal(_state.State.Status) || _state.State.OrderId == Guid.Empty)
        {
            return ToSnapshot();
        }

        await _executionRouter.CancelOrderAsync(_state.State.VenueType, _state.State.OrderId, _state.State.Symbol, reason);

        var order = await _orderRepository.GetByIdAsync(OrderId.From(_state.State.OrderId));
        if (order != null && !order.IsTerminal)
        {
            order.Cancel(reason);
            await _orderRepository.AppendEventsAsync(order);
        }

        _state.State.Status = OrderStatus.Cancelled;
        _state.State.RejectionReason = reason;
        _state.State.LastUpdatedAtUtc = DateTimeOffset.UtcNow;

        await UnregisterSessionAndExpiryAsync();
        await _state.WriteStateAsync();

        await _messageBus.PublishAsync(new OrderSettlementFailedEvent(_state.State.OrderId, reason));
        await PublishAuditAsync("OrderCancelled", null, _state.State.Price, _state.State.Quantity);

        return ToSnapshot();
    }

    public async Task<OrderStateSnapshot> ExpireOrderAsync()
    {
        if (IsTerminal(_state.State.Status) || _state.State.OrderId == Guid.Empty)
        {
            return ToSnapshot();
        }

        const string reason = "Order expired (Good-Til-Time elapsed)";
        await _executionRouter.CancelOrderAsync(_state.State.VenueType, _state.State.OrderId, _state.State.Symbol, reason);

        var order = await _orderRepository.GetByIdAsync(OrderId.From(_state.State.OrderId));
        if (order != null && !order.IsTerminal)
        {
            order.Expire();
            await _orderRepository.AppendEventsAsync(order);
        }

        _state.State.Status = OrderStatus.Expired;
        _state.State.RejectionReason = reason;
        _state.State.LastUpdatedAtUtc = DateTimeOffset.UtcNow;

        var clientSession = GrainFactory.GetGrain<IClientSessionGrain>(_state.State.ClientId.ToString("N"));
        await clientSession.UnregisterOrderAsync(_state.State.OrderId);
        await _state.WriteStateAsync();

        await _messageBus.PublishAsync(new OrderSettlementFailedEvent(_state.State.OrderId, reason));
        await PublishAuditAsync("OrderExpired", null, _state.State.Price, _state.State.Quantity);

        return ToSnapshot();
    }

    public async Task<OrderStateSnapshot> ApplyExecutionReportAsync(decimal fillQuantity, decimal fillPrice, string tradeId)
    {
        if (IsTerminal(_state.State.Status) || _state.State.OrderId == Guid.Empty)
        {
            return ToSnapshot();
        }

        var parsedTradeId = Guid.TryParse(tradeId, out var tid) ? tid : Guid.NewGuid();
        var order = await _orderRepository.GetByIdAsync(OrderId.From(_state.State.OrderId));
        if (order != null && !order.IsTerminal)
        {
            order.ApplyExecution(TradeId.From(parsedTradeId), new Quantity(fillQuantity), new Price(fillPrice), _state.State.VenueType);
            await _orderRepository.AppendEventsAsync(order);
        }

        var totalCost = (_state.State.CumulativeFilledQuantity * _state.State.AverageFilledPrice) + (fillQuantity * fillPrice);
        _state.State.CumulativeFilledQuantity += fillQuantity;
        _state.State.AverageFilledPrice = _state.State.CumulativeFilledQuantity > 0
            ? totalCost / _state.State.CumulativeFilledQuantity
            : 0m;

        if (_state.State.CumulativeFilledQuantity >= _state.State.Quantity)
        {
            _state.State.Status = OrderStatus.Filled;
            await UnregisterSessionAndExpiryAsync();
        }
        else
        {
            _state.State.Status = OrderStatus.PartiallyFilled;
        }

        _state.State.LastUpdatedAtUtc = DateTimeOffset.UtcNow;
        await _state.WriteStateAsync();
        await PublishAuditAsync("OrderExecuted", parsedTradeId, fillPrice, fillQuantity);

        return ToSnapshot();
    }

    public Task<OrderStateSnapshot> GetSnapshotAsync() => Task.FromResult(ToSnapshot());

    private void ApplyDomainStateToGrain(SubmitOrderRequest request, Order order)
    {
        _state.State.OrderId = request.OrderId;
        _state.State.ClientId = request.ClientId;
        _state.State.Symbol = request.Symbol;
        _state.State.Side = request.Side;
        _state.State.Type = request.Type;
        _state.State.Quantity = request.Quantity;
        _state.State.Price = request.Price;
        _state.State.TimeInForce = request.TimeInForce;
        _state.State.VenueType = request.VenueType;
        _state.State.Status = order.Status;
        _state.State.CreatedAtUtc = order.CreatedAtUtc;
        _state.State.LastUpdatedAtUtc = order.UpdatedAtUtc;
    }

    private async Task UnregisterSessionAndExpiryAsync()
    {
        var clientSession = GrainFactory.GetGrain<IClientSessionGrain>(_state.State.ClientId.ToString("N"));
        await clientSession.UnregisterOrderAsync(_state.State.OrderId);

        if (_state.State.TimeInForce == TimeInForce.GoodTilTime)
        {
            var expiryGrain = GrainFactory.GetGrain<IOrderExpiryGrain>(_state.State.OrderId);
            await expiryGrain.CancelOrderExpiryAsync();
        }
    }

    private Task PublishAuditAsync(string eventType, Guid? tradeId, decimal price, decimal quantity) =>
        _messageBus.PublishAsync(new AuditTrailEvent(
            eventType,
            _state.State.OrderId,
            tradeId,
            _state.State.ClientId,
            _state.State.Symbol,
            price,
            quantity,
            JsonSerializer.Serialize(ToSnapshot()),
            DateTimeOffset.UtcNow)).AsTask();

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
