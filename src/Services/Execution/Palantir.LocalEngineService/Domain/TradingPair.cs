using Palantir.SharedKernel;

namespace Palantir.LocalEngineService.Domain;

public class TradingPair : Entity<string>, IAuditableEntity
{
    public string Symbol => Id;
    public string BaseCurrency { get; set; } = string.Empty;
    public string QuoteCurrency { get; set; } = string.Empty;
    public decimal TickSize { get; set; } = 0.01m;
    public decimal LotSize { get; set; } = 0.001m;
    public decimal MinQuantity { get; set; } = 0.001m;
    public decimal MaxQuantity { get; set; } = 1000000m;
    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedAtUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    public TradingPair() { }

    public TradingPair(string symbol, string baseCurrency, string quoteCurrency, decimal tickSize, decimal lotSize)
    {
        Id = symbol.ToUpperInvariant();
        BaseCurrency = baseCurrency.ToUpperInvariant();
        QuoteCurrency = quoteCurrency.ToUpperInvariant();
        TickSize = tickSize;
        LotSize = lotSize;
    }
}
