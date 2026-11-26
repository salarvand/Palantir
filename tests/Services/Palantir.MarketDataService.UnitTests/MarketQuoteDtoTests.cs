using Palantir.MarketDataService.Features.Quotes.Get;
using Shouldly;
using Xunit;

namespace Palantir.MarketDataService.UnitTests;

public sealed class MarketQuoteDtoTests
{
    [Fact]
    public void MarketQuoteDto_ShouldMaintainBidAskSpreadInvariant()
    {
        var quote = new MarketQuoteDto("PAL/USD", 100.50m, 100.55m, 100.52m, DateTimeOffset.UtcNow);
        quote.Ask.ShouldBeGreaterThan(quote.Bid);
        quote.Symbol.ShouldBe("PAL/USD");
    }
}
