using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Palantir.IamService.Infrastructure;
using Palantir.Platform.Endpoints;
using Palantir.Platform.Security;

namespace Palantir.IamService.Features.Tokens.Exchange;

internal sealed class ExchangeTokenEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/iam/token/exchange", async (
                Guid userId,
                IamDbContext db,
                IPermissionTokenManager tokenManager,
                CancellationToken ct) =>
            {
                var cached = await tokenManager.GetPermissionTokenAsync(userId, ct);
                if (cached is not null) return TypedResults.Ok(cached);

                var profile = await db.Profiles.FindAsync([userId], ct);
                if (profile is null) return Results.NotFound("User profile not found.");

                var token = new PermissionToken(
                    profile.Id,
                    profile.Username,
                    [profile.Role],
                    profile.AssignedPermissions,
                    DateTimeOffset.UtcNow,
                    DateTimeOffset.UtcNow.AddMinutes(30));

                await tokenManager.SetPermissionTokenAsync(token, TimeSpan.FromMinutes(30), ct);
                return TypedResults.Ok(token);
            })
            .ProducesPostWithoutLocation<PermissionToken>()
            .WithTags("IAM")
            .WithName(nameof(ExchangeTokenEndpoint))
            .MapToApiVersion(ApiVersioningExtensions.V1)
            .RequireAuthorization();
    }
}
