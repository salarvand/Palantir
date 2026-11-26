using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Palantir.Constants;
using Palantir.Platform.Endpoints;
using Palantir.PortfolioService.Infrastructure;

namespace Palantir.PortfolioService.Features.Margin.Reserve;

internal sealed class ReserveMarginEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/portfolio/{accountId:guid}/margin/reserve", async (
                Guid accountId,
                decimal amount,
                PortfolioDbContext db,
                CancellationToken ct) =>
            {
                if (amount <= 0)
                {
                    return Results.BadRequest("Margin reserve amount must be positive.");
                }

                var account = await db.Accounts.FindAsync([accountId], ct);
                if (account is null) return Results.NotFound();

                if (account.AvailableMargin < amount)
                {
                    return Results.BadRequest($"Insufficient available margin (${account.AvailableMargin:N2}) to reserve ${amount:N2}.");
                }

                account.ReservedMargin += amount;
                await db.SaveChangesAsync(ct);

                return TypedResults.Ok(new { account.Id, account.ReservedMargin, account.AvailableMargin });
            })
            .ProducesPostWithoutLocation<object>()
            .WithTags("Portfolio & Ledger")
            .WithName(nameof(ReserveMarginEndpoint))
            .MapToApiVersion(ApiVersioningExtensions.V1)
            .RequireAuthorization(Permissions.AllowAdjustBalance);
    }
}
