using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Palantir.Platform.Security.Permissions;

public sealed class DimensionRequirement(
    PermissionDimension dimension,
    string action,
    string? resourceType = null) : IAuthorizationRequirement
{
    public PermissionDimension Dimension { get; } = dimension;
    public string Action { get; } = action;
    public string? ResourceType { get; } = resourceType;
}

public sealed class DimensionAuthorizationHandler(
    IPermissionEvaluator evaluator,
    IHttpContextAccessor httpContextAccessor) : AuthorizationHandler<DimensionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DimensionRequirement requirement)
    {
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? context.User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return;
        }

        if (context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return;
        }

        var permContext = PermissionContext.FromHttpContext(httpContextAccessor.HttpContext, userIdClaim);
        var resource = ResourceTemplate.Any(requirement.ResourceType ?? requirement.Dimension.Name);

        var result = await evaluator.EvaluateAsync(
            userId,
            requirement.Dimension,
            requirement.Action,
            resource,
            permContext);

        if (result.IsAllowed)
        {
            context.Succeed(requirement);
        }
    }
}

public static class DimensionAuthorizationExtensions
{
    public static RouteHandlerBuilder RequireDimension(
        this RouteHandlerBuilder builder,
        PermissionDimension dimension,
        string action,
        string? resourceType = null) =>
        builder.RequireAuthorization(policy =>
            policy.Requirements.Add(new DimensionRequirement(dimension, action, resourceType)));
}
