using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Palantir.Constants;
using Palantir.Platform.Endpoints;
using Palantir.PortfolioService.Infrastructure;

namespace Palantir.PortfolioService.Features.Accounts.Get;

internal sealed class GetPortfolioEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/portfolio/{accountId:guid}", async (Guid accountId, PortfolioDbContext db, CancellationToken ct) =>
            {
                var account = await db.Accounts
                    .AsNoTracking()
                    .Include(a => a.Positions)
                    .FirstOrDefaultAsync(a => a.Id == accountId, ct);

                if (account is null)
                {
                    return Results.NotFound();
                }

                return TypedResults.Ok(new
                {
                    account.Id,
                    account.UserId,
                    account.Currency,
                    account.CashBalance,
                    account.ReservedMargin,
                    account.AvailableMargin,
                    TotalUnrealizedPnL = account.Positions.Sum(p => p.UnrealizedPnL),
                    Positions = account.Positions.Select(p => new
                    {
                        p.Id,
                        p.Symbol,
                        p.Quantity,
                        p.AverageEntryPrice,
                        p.CurrentPrice,
                        p.UnrealizedPnL
                    })
                });
            })
            .ProducesGet<object>(hasNotFound: true)
            .WithTags("Portfolio & Ledger")
            .WithName(nameof(GetPortfolioEndpoint))
            .MapToApiVersion(ApiVersioningExtensions.V1)
            .RequireAuthorization(Permissions.AllowViewPortfolio);
    }
}
