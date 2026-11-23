namespace Palantir.SharedKernel;

public sealed class Order : AggregateRoot<OrderId>
{
    public ClientId ClientId { get; private set; }
    public SymbolId Symbol { get; private set; }
    public TradeSide Side { get; private set; }
    public OrderType Type { get; private set; }
    public Quantity Quantity { get; private set; }
    public Price Price { get; private set; }
    public TimeInForce TimeInForce { get; private set; }
    public OrderStatus Status { get; private set; }
    public Quantity CumulativeFilledQuantity { get; private set; }
    public Price AverageFilledPrice { get; private set; }
    public decimal ReservedMargin { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    private readonly List<Trade> _trades = [];
    public IReadOnlyCollection<Trade> Trades => _trades.AsReadOnly();

    public Quantity RemainingQuantity => new(Math.Max(0m, Quantity.Value - CumulativeFilledQuantity.Value));

    public bool IsTerminal => Status is OrderStatus.Filled or OrderStatus.Cancelled or OrderStatus.Rejected or OrderStatus.Expired;

    public Order() { }

    public static Order Create(
        OrderId orderId,
        ClientId clientId,
        SymbolId symbol,
        TradeSide side,
        OrderType type,
        Quantity quantity,
        Price price,
        TimeInForce timeInForce,
        DateTimeOffset? timestampUtc = null)
    {
        if (quantity.Value <= 0)
            throw new ArgumentException("Order quantity must be positive.", nameof(quantity));

        if (type is OrderType.Limit or OrderType.StopLimit && price.Value <= 0)
            throw new ArgumentException("Limit orders must specify a positive price.", nameof(price));

        var now = timestampUtc ?? DateTimeOffset.UtcNow;
        var order = new Order
        {
            Id = orderId,
            ClientId = clientId,
            Symbol = symbol,
            Side = side,
            Type = type,
            Quantity = quantity,
            Price = price,
            TimeInForce = timeInForce,
            Status = OrderStatus.New,
            CumulativeFilledQuantity = Quantity.Zero,
            AverageFilledPrice = Price.Zero,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var @event = new OrderCreatedDomainEvent(
            orderId, clientId, symbol, side, type, quantity, price, timeInForce, now
        );
        order.AddDomainEvent(@event);

        return order;
    }

    public void ApproveRisk(decimal reservedMargin, DateTimeOffset? approvedAt = null)
    {
        if (IsTerminal)
            throw new InvalidOperationException($"Cannot approve risk on an order in terminal state '{Status}'.");

        var now = approvedAt ?? DateTimeOffset.UtcNow;
        ReservedMargin = reservedMargin;
        Status = OrderStatus.Submitted;
        UpdatedAtUtc = now;

        AddDomainEvent(new OrderRiskApprovedDomainEvent(Id, reservedMargin, now));
    }

    public void ApplyExecution(
        TradeId tradeId,
        Quantity fillQuantity,
        Price fillPrice,
        string venue = "INTERNAL",
        DateTimeOffset? executedAt = null)
    {
        if (IsTerminal)
            throw new InvalidOperationException($"Cannot execute an order in terminal state '{Status}'.");

        if (fillQuantity.Value <= 0)
            throw new ArgumentException("Fill quantity must be positive.", nameof(fillQuantity));

        if (fillQuantity.Value > RemainingQuantity.Value)
            throw new InvalidOperationException($"Fill quantity ({fillQuantity.Value}) exceeds remaining order quantity ({RemainingQuantity.Value}).");

        var now = executedAt ?? DateTimeOffset.UtcNow;
        var trade = new Trade(tradeId, Id, venue, fillPrice, fillQuantity, now);
        _trades.Add(trade);

        // Weighted average price calculation
        var previousTotalValue = CumulativeFilledQuantity.Value * AverageFilledPrice.Value;
        var newTotalValue = previousTotalValue + (fillQuantity.Value * fillPrice.Value);
        var totalQuantity = CumulativeFilledQuantity.Value + fillQuantity.Value;

        CumulativeFilledQuantity = new Quantity(totalQuantity);
        AverageFilledPrice = totalQuantity > 0 ? new Price(newTotalValue / totalQuantity) : Price.Zero;

        Status = CumulativeFilledQuantity.Value >= Quantity.Value
            ? OrderStatus.Filled
            : OrderStatus.PartiallyFilled;

        UpdatedAtUtc = now;

        AddDomainEvent(new OrderExecutionAppliedDomainEvent(
            Id, tradeId, fillQuantity, fillPrice, RemainingQuantity, Status, now
        ));
    }

    public void Cancel(string reason, DateTimeOffset? cancelledAt = null)
    {
        if (IsTerminal)
            throw new InvalidOperationException($"Cannot cancel an order in terminal state '{Status}'.");

        var now = cancelledAt ?? DateTimeOffset.UtcNow;
        Status = OrderStatus.Cancelled;
        UpdatedAtUtc = now;

        AddDomainEvent(new OrderCancelledDomainEvent(Id, reason, now));
    }

    public void Reject(string reason, DateTimeOffset? rejectedAt = null)
    {
        if (IsTerminal)
            throw new InvalidOperationException($"Cannot reject an order in terminal state '{Status}'.");

        var now = rejectedAt ?? DateTimeOffset.UtcNow;
        Status = OrderStatus.Rejected;
        UpdatedAtUtc = now;

        AddDomainEvent(new OrderRejectedDomainEvent(Id, reason, now));
    }

    public void Expire(DateTimeOffset? expiredAt = null)
    {
        if (IsTerminal)
            throw new InvalidOperationException($"Cannot expire an order in terminal state '{Status}'.");

        var now = expiredAt ?? DateTimeOffset.UtcNow;
        Status = OrderStatus.Expired;
        UpdatedAtUtc = now;

        AddDomainEvent(new OrderExpiredDomainEvent(Id, now));
    }

    public void Apply(IDomainEvent domainEvent)
    {
        switch (domainEvent)
        {
            case OrderCreatedDomainEvent created:
                Id = created.OrderId;
                ClientId = created.ClientId;
                Symbol = created.Symbol;
                Side = created.Side;
                Type = created.Type;
                Quantity = created.Quantity;
                Price = created.Price;
                TimeInForce = created.TimeInForce;
                Status = OrderStatus.New;
                CumulativeFilledQuantity = Quantity.Zero;
                AverageFilledPrice = Price.Zero;
                CreatedAtUtc = created.TimestampUtc;
                UpdatedAtUtc = created.TimestampUtc;
                break;

            case OrderRiskApprovedDomainEvent riskApproved:
                ReservedMargin = riskApproved.ReservedMargin;
                Status = OrderStatus.Submitted;
                UpdatedAtUtc = riskApproved.TimestampUtc;
                break;

            case OrderExecutionAppliedDomainEvent executed:
                var trade = new Trade(executed.TradeId, Id, "INTERNAL", executed.FillPrice, executed.FillQuantity, executed.TimestampUtc);
                _trades.Add(trade);
                var prevVal = CumulativeFilledQuantity.Value * AverageFilledPrice.Value;
                var newVal = prevVal + (executed.FillQuantity.Value * executed.FillPrice.Value);
                var totalQty = CumulativeFilledQuantity.Value + executed.FillQuantity.Value;
                CumulativeFilledQuantity = new Quantity(totalQty);
                AverageFilledPrice = totalQty > 0 ? new Price(newVal / totalQty) : Price.Zero;
                Status = executed.Status;
                UpdatedAtUtc = executed.TimestampUtc;
                break;

            case OrderCancelledDomainEvent cancelled:
                Status = OrderStatus.Cancelled;
                UpdatedAtUtc = cancelled.TimestampUtc;
                break;

            case OrderRejectedDomainEvent rejected:
                Status = OrderStatus.Rejected;
                UpdatedAtUtc = rejected.TimestampUtc;
                break;

            case OrderExpiredDomainEvent expired:
                Status = OrderStatus.Expired;
                UpdatedAtUtc = expired.TimestampUtc;
                break;
        }
    }
}
