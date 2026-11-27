using Palantir.SharedKernel;

namespace Palantir.Platform.Security.Permissions;

/// <summary>
/// Contract for specification-based permission condition evaluators.
/// </summary>
public interface IPermissionSpecification : IValidationSpecification<PermissionEvaluationRequest>
{
}

public sealed record PermissionGrant(
    PermissionDimension Dimension,
    string Action,
    string? ResourceType = null,
    string? ResourceId = null,
    IReadOnlyDictionary<string, string>? Conditions = null);

public sealed record PermissionEvaluationRequest(
    PermissionDimension Dimension,
    string Action,
    ResourceTemplate Resource,
    PermissionContext Context,
    PermissionGrant Grant);

public sealed record PermissionEvaluationResult(
    bool IsAllowed,
    string? DenialReason = null);
