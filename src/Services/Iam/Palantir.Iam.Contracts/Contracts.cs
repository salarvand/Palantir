namespace Palantir.Iam.Contracts;

public record UserCreatedIntegrationEvent(
    Guid UserId,
    string Username,
    string Email,
    string Role,
    DateTimeOffset TimestampUtc
);

public record UserStatusChangedIntegrationEvent(
    Guid UserId,
    bool IsActive,
    DateTimeOffset TimestampUtc
);

public record PermissionsUpdatedIntegrationEvent(
    Guid UserId,
    IReadOnlyList<string> Permissions,
    DateTimeOffset TimestampUtc
);

public record CreateUserRequest(
    string Username,
    string Email,
    string FirstName,
    string LastName,
    string Password,
    string Role,
    int KycTier,
    decimal MaxLeverage,
    decimal DailyLossLimit
);

public record UserDto(
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
    IReadOnlyList<string> Permissions
);

public record AssignPermissionsRequest(
    IReadOnlyList<string> Permissions
);
