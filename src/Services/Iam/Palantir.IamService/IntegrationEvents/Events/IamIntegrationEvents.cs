using Wolverine.Attributes;

namespace Palantir.IamService.IntegrationEvents.Events;

[MessageIdentity("palantir.user-created")]
public sealed record UserCreatedIntegrationEvent(
    Guid UserId,
    string Username,
    string Email,
    string Role,
    DateTimeOffset TimestampUtc);

[MessageIdentity("palantir.permissions-updated")]
public sealed record PermissionsUpdatedIntegrationEvent(
    Guid UserId,
    IReadOnlyList<string> Permissions,
    DateTimeOffset TimestampUtc);
