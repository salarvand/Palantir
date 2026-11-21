using Palantir.SharedKernel;

namespace Palantir.PortfolioService.Domain;

public class Account : Entity<Guid>, IAuditableEntity
{
    public Guid UserId { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal CashBalance { get; set; } = 100000m;
    public decimal ReservedMargin { get; set; } = 0m;
    public decimal AvailableMargin => CashBalance - ReservedMargin;

    public List<Position> Positions { get; set; } = [];

    public DateTimeOffset CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedAtUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    public Account() { }

    public Account(Guid accountId, Guid userId, decimal initialCash, string currency = "USD")
    {
        Id = accountId;
        UserId = userId;
        CashBalance = initialCash;
        Currency = currency;
    }
}

public class Position : Entity<Guid>
{
    public Guid AccountId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal AverageEntryPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal UnrealizedPnL => (CurrentPrice - AverageEntryPrice) * Quantity;
}
