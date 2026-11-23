namespace Palantir.SharedKernel;

public sealed class Trade : Entity<TradeId>
{
    public OrderId OrderId { get; private set; }
    public string ExecutionVenue { get; private set; }
    public Price Price { get; private set; }
    public Quantity Quantity { get; private set; }
    public DateTimeOffset ExecutedAtUtc { get; private set; }

    public Trade(
        TradeId id,
        OrderId orderId,
        string executionVenue,
        Price price,
        Quantity quantity,
        DateTimeOffset executedAtUtc)
    {
        if (price.Value <= 0)
            throw new ArgumentException("Execution price must be positive.", nameof(price));
        if (quantity.Value <= 0)
            throw new ArgumentException("Execution quantity must be positive.", nameof(quantity));

        Id = id;
        OrderId = orderId;
        ExecutionVenue = executionVenue ?? "INTERNAL";
        Price = price;
        Quantity = quantity;
        ExecutedAtUtc = executedAtUtc;
    }
}
