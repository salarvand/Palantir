namespace Palantir.SharedKernel;

public enum TradeSide
{
    Buy = 1,
    Sell = 2
}

public enum OrderType
{
    Market = 1,
    Limit = 2,
    StopLimit = 3
}

public enum OrderStatus
{
    New = 1,
    Submitted = 2,
    Accepted = 3,
    PartiallyFilled = 4,
    Filled = 5,
    Cancelled = 6,
    Rejected = 7,
    Expired = 8
}

public enum TimeInForce
{
    GoodTilCancelled = 1,
    ImmediateOrCancel = 2,
    FillOrKill = 3,
    GoodTilTime = 4
}

public readonly record struct Money(decimal Amount, string Currency)
{
    public static Money Zero(string currency) => new(0m, currency);

    public static Money operator +(Money left, Money right)
    {
        if (!string.Equals(left.Currency, right.Currency, StringComparison.OrdinalIgnoreCase))
            throw exciting(left.Currency, right.Currency);
        return new Money(left.Amount + right.Amount, left.Currency);
    }

    public static Money operator -(Money left, Money right)
    {
        if (!string.Equals(left.Currency, right.Currency, StringComparison.OrdinalIgnoreCase))
            throw exciting(left.Currency, right.Currency);
        return new Money(left.Amount - right.Amount, left.Currency);
    }

    private static InvalidOperationException exciting(string c1, string c2) =>
        new($"Cannot operate on money with different currencies: '{c1}' vs '{c2}'.");
}

public readonly record struct Price(decimal Value)
{
    public static Price Zero => new(0m);
    public static implicit operator decimal(Price price) => price.Value;
    public static implicit operator Price(decimal value) => new(value);
}

public readonly record struct Quantity(decimal Value)
{
    public static Quantity Zero => new(0m);
    public static implicit operator decimal(Quantity qty) => qty.Value;
    public static implicit operator Quantity(decimal value) => new(value);
}
