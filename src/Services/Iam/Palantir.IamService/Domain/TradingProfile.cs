using Palantir.SharedKernel;

namespace Palantir.IamService.Domain;

public class TradingProfile : Entity<Guid>, IAuditableEntity
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int KycTier { get; set; } = 1;
    public decimal MaxLeverage { get; set; } = 10m;
    public decimal DailyLossLimit { get; set; } = 50000m;
    public Guid TradingAccountId { get; set; } = Guid.NewGuid();
    public List<string> AssignedPermissions { get; set; } = [];

    public DateTimeOffset CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedAtUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    public TradingProfile() { }

    public TradingProfile(Guid userId, string username, string email, string role, int kycTier, decimal maxLeverage, decimal dailyLossLimit)
    {
        Id = userId;
        Username = username;
        Email = email;
        Role = role;
        KycTier = kycTier;
        MaxLeverage = maxLeverage;
        DailyLossLimit = dailyLossLimit;
    }
}
