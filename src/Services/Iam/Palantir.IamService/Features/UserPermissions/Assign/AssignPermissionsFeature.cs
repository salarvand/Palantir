using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Palantir.Constants;
using Palantir.IamService.Infrastructure;
using Palantir.IamService.IntegrationEvents.Events;
using Palantir.Platform.Endpoints;
using Palantir.Platform.Security;
using Wolverine;

namespace Palantir.IamService.Features.UserPermissions.Assign;

public sealed record AssignPermissionsRequest(IReadOnlyList<string> Permissions);

internal sealed class AssignPermissionsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/iam/users/{id:guid}/permissions", async (
                Guid id,
                AssignPermissionsRequest request,
                IamDbContext db,
                IPermissionTokenManager tokenManager,
                IMessageBus messageBus,
                CancellationToken ct) =>
            {
                var profile = await db.Profiles.FindAsync([id], ct);
                if (profile is null) return Results.NotFound();

                profile.AssignedPermissions = request.Permissions.ToList();
                await db.SaveChangesAsync(ct);

                var token = new PermissionToken(
                    profile.Id,
                    profile.Username,
                    [profile.Role],
                    profile.AssignedPermissions,
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow.AddMinutes(30));

                await tokenManager.SetPermissionTokenAsync(token, TimeSpan.FromMinutes(30), ct);

                await messageBus.PublishAsync(new PermissionsUpdatedIntegrationEvent(
                    profile.Id,
                    profile.AssignedPermissions,
                    DateTimeOffset.UtcNow));

                return TypedResults.Ok(profile.AssignedPermissions);
            })
            .WithTags("IAM")
            .WithName(nameof(AssignPermissionsEndpoint))
            .MapToApiVersion(ApiVersioningExtensions.V1)
            .RequireAuthorization(Permissions.AllowManageRoles);
    }
}
