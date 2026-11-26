using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Palantir.Constants;
using Palantir.IamService.Infrastructure;
using Palantir.Platform.Endpoints;

namespace Palantir.IamService.Features.Users.Get;

internal sealed class GetUserByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/iam/users/{id:guid}", async (Guid id, IamDbContext db, CancellationToken ct) =>
            {
                var profile = await db.Profiles.FindAsync([id], ct);
                if (profile is null) return Results.NotFound();

                return TypedResults.Ok(new UserDto(
                    profile.Id,
                    profile.Username,
                    profile.Email,
                    string.Empty,
                    string.Empty,
                    true,
                    profile.Role,
                    profile.KycTier,
                    profile.MaxLeverage,
                    profile.DailyLossLimit,
                    profile.AssignedPermissions));
            })
            .ProducesGet<UserDto>(hasNotFound: true)
            .WithTags("IAM")
            .WithName(nameof(GetUserByIdEndpoint))
            .MapToApiVersion(ApiVersioningExtensions.V1)
            .RequireAuthorization(Permissions.AllowManageUsers);
    }
}
