using Palantir.SharedKernel;

namespace Palantir.Platform.Security.Permissions;

public interface IPermissionEvaluator
{
    Task<PermissionEvaluationResult> EvaluateAsync(
        Guid userId,
        PermissionDimension dimension,
        string action,
        ResourceTemplate resource,
        PermissionContext? context = null,
        CancellationToken ct = default);

    PermissionEvaluationResult EvaluateGrants(
        IEnumerable<PermissionGrant> grants,
        PermissionDimension dimension,
        string action,
        ResourceTemplate resource,
        PermissionContext context);
}

/// <summary>
/// Evaluates permission grants against composed <see cref="IPermissionSpecification"/> rules.
/// </summary>
public sealed class PermissionEvaluator(
    IPermissionTokenManager tokenManager,
    IEnumerable<IPermissionSpecification> specifications) : IPermissionEvaluator
{
    private readonly IPermissionSpecification[] _specifications = specifications.ToArray();

    public async Task<PermissionEvaluationResult> EvaluateAsync(
        Guid userId,
        PermissionDimension dimension,
        string action,
        ResourceTemplate resource,
        PermissionContext? context = null,
        CancellationToken ct = default)
    {
        var token = await tokenManager.GetPermissionTokenAsync(userId, ct);
        if (token is null)
        {
            return new PermissionEvaluationResult(false, "No permission token found.");
        }

        var effectiveContext = (context ?? PermissionContext.Default()) with
        {
            SubjectUserId = userId.ToString()
        };

        return EvaluateGrants(ParseGrants(token), dimension, action, resource, effectiveContext);
    }

    public PermissionEvaluationResult EvaluateGrants(
        IEnumerable<PermissionGrant> grants,
        PermissionDimension dimension,
        string action,
        ResourceTemplate resource,
        PermissionContext context)
    {
        foreach (var grant in grants)
        {
            var request = new PermissionEvaluationRequest(dimension, action, resource, context, grant);
            var validation = request.Validate(_specifications);
            if (validation.IsValid)
            {
                return new PermissionEvaluationResult(true);
            }
        }

        return new PermissionEvaluationResult(false, $"No grant satisfies {dimension.Name}:{action} on {resource.Type}.");
    }

    public static (PermissionDimension Dimension, string Action, string? ResourceType) ParsePermissionString(string permission)
    {
        var parts = permission.Split('.', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 3
            ? (PermissionDimension.FromName(parts[1]), parts[2], parts.Length > 3 ? parts[3] : null)
            : (PermissionDimension.FromName("Unknown"), permission, null);
    }

    private static IEnumerable<PermissionGrant> ParseGrants(PermissionToken token) =>
        token.Permissions.Select(p =>
        {
            var (dimension, action, resourceType) = ParsePermissionString(p);
            return new PermissionGrant(dimension, action, resourceType);
        });
}
