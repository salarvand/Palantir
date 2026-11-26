namespace Palantir.IamService.Features.Users;

public sealed record UserDto(
    Guid UserId,
    string Username,
    string Email,
    string FirstName,
    string LastName,
    bool Enabled,
    string Role,
    int KycTier,
    decimal MaxLeverage,
    decimal DailyLossLimit,
    IReadOnlyList<string> Permissions);
