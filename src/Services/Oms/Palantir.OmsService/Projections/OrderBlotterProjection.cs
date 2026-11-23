using Marten.Events.Aggregation;
using Palantir.SharedKernel;

namespace Palantir.OmsService.Projections;

public class OrderBlotterView
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Side { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal FilledQuantity { get; set; }
    public decimal AveragePrice { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public class OrderBlotterProjection : SingleStreamProjection<OrderBlotterView>
{
    public OrderBlotterView Create(OrderCreatedDomainEvent @event)
    {
        return new OrderBlotterView
        {
            Id = @event.OrderId.Value,
            ClientId = @event.ClientId.Value,
            Symbol = @event.Symbol.Value,
            Side = @event.Side.ToString(),
            Type = @event.Type.ToString(),
            Quantity = @event.Quantity.Value,
            Price = @event.Price.Value,
            Status = OrderStatus.New.ToString(),
            FilledQuantity = 0,
            AveragePrice = 0,
            CreatedAtUtc = @event.TimestampUtc,
            UpdatedAtUtc = @event.TimestampUtc
        };
    }

    public void Apply(OrderRiskApprovedDomainEvent @event, OrderBlotterView view)
    {
        view.Status = OrderStatus.Submitted.ToString();
        view.UpdatedAtUtc = @event.TimestampUtc;
    }

    public void Apply(OrderExecutionAppliedDomainEvent @event, OrderBlotterView view)
    {
        view.FilledQuantity += @event.FillQuantity.Value;
        view.AveragePrice = @event.FillPrice.Value;
        view.Status = @event.Status.ToString();
        view.UpdatedAtUtc = @event.TimestampUtc;
    }

    public void Apply(OrderCancelledDomainEvent @event, OrderBlotterView view)
    {
        view.Status = OrderStatus.Cancelled.ToString();
        view.UpdatedAtUtc = @event.TimestampUtc;
    }

    public void Apply(OrderRejectedDomainEvent @event, OrderBlotterView view)
    {
        view.Status = OrderStatus.Rejected.ToString();
        view.UpdatedAtUtc = @event.TimestampUtc;
    }

    public void Apply(OrderExpiredDomainEvent @event, OrderBlotterView view)
    {
        view.Status = OrderStatus.Expired.ToString();
        view.UpdatedAtUtc = @event.TimestampUtc;
    }
}
