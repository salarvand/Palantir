using Bogus;
using Palantir.Constants;
using Palantir.SharedKernel;

namespace Palantir.Testing.Shared;

public static class DataFakers
{
    private static readonly string[] Symbols = ["PAL/USD", "BTC/USD", "ETH/USD", "GOLD/USDT", "EUR/USD"];

    public static Faker<TradingValueObjectsFaker> TradingFaker => new Faker<TradingValueObjectsFaker>()
        .RuleFor(f => f.Symbol, f => f.PickRandom(Symbols))
        .RuleFor(f => f.Price, f => Math.Round(f.Random.Decimal(10m, 500m), 2))
        .RuleFor(f => f.Quantity, f => Math.Round(f.Random.Decimal(1m, 100m), 2))
        .RuleFor(f => f.Side, f => f.PickRandom<TradeSide>());
}

public class TradingValueObjectsFaker
{
    public string Symbol { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Quantity { get; set; }
    public TradeSide Side { get; set; }
}
