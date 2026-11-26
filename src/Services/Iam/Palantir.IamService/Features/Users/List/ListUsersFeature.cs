using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Palantir.Constants;
using Palantir.IamService.Infrastructure;
using Palantir.Platform.Endpoints;

namespace Palantir.IamService.Features.Users.List;

internal sealed class ListUsersEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/iam/users", async (IamDbContext db, CancellationToken ct) =>
            {
                var profiles = await db.Profiles.AsNoTracking().ToListAsync(ct);
                var dtos = profiles.Select(p => new UserDto(
                    p.Id,
                    p.Username,
                    p.Email,
                    string.Empty,
                    string.Empty,
                    true,
                    p.Role,
                    p.KycTier,
                    p.MaxLeverage,
                    p.DailyLossLimit,
                    p.AssignedPermissions)).ToList();

                return TypedResults.Ok(dtos);
            })
            .ProducesGet<List<UserDto>>()
            .WithTags("IAM")
            .WithName(nameof(ListUsersEndpoint))
            .MapToApiVersion(ApiVersioningExtensions.V1)
            .RequireAuthorization(Permissions.AllowManageUsers);
    }
}
