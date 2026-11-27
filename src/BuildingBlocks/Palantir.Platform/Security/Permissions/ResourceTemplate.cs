namespace Palantir.Platform.Security.Permissions;

/// <summary>
/// Target resource descriptor supporting wildcard type, specific entity ID, or ownership checks.
/// </summary>
public sealed record ResourceTemplate(
    string Type,
    string? Id = null,
    string? OwnerId = null)
{
    public static ResourceTemplate Any(string type) => new(type);
    public static ResourceTemplate Specific(string type, string id) => new(type, id);
    public static ResourceTemplate OwnedBy(string type, string ownerId, string? id = null) => new(type, id, ownerId);
}
