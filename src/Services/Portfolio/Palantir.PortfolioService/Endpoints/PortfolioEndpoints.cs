using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Palantir.Constants;
using Palantir.Platform.Endpoints;
using Palantir.PortfolioService.Infrastructure;

namespace Palantir.PortfolioService.Endpoints;

public class PortfolioEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/portfolio")
            .WithTags("Portfolio & Ledger");

        group.MapGet("/{accountId:guid}", GetPortfolioAsync)
            .RequireAuthorization(Permissions.AllowViewPortfolio);

        group.MapPost("/{accountId:guid}/margin/reserve", ReserveMarginAsync);

        group.MapPost("/{accountId:guid}/margin/release", ReleaseMarginAsync);
    }

    private static async Task<IResult> GetPortfolioAsync(Guid accountId, PortfolioDbContext db, CancellationToken ct)
    {
        var account = await db.Accounts
            .Include(a => a.Positions)
            .FirstOrDefaultAsync(a => a.Id == accountId, ct);

        if (account == null)
        {
            // Seed a default demo account for testing if none found
            account = new Domain.Account(accountId, Guid.NewGuid(), 100000m);
            account.Positions.Add(new Domain.Position
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                Symbol = "PAL/USD",
                Quantity = 500m,
                AverageEntryPrice = 100.00m,
                CurrentPrice = 105.50m
            });
            db.Accounts.Add(account);
            await db.SaveChangesAsync(ct);
        }

        return Results.Ok(new
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
    }

    private static async Task<IResult> ReserveMarginAsync(
        Guid accountId,
        decimal amount,
        PortfolioDbContext db,
        CancellationToken ct)
    {
        var account = await db.Accounts.FindAsync([accountId], ct);
        if (account == null) return Results.NotFound();

        if (account.AvailableMargin < amount)
        {
            return Results.BadRequest($"Insufficient available margin (${account.AvailableMargin:N2}) to reserve ${amount:N2}.");
        }

        account.ReservedMargin += amount;
        await db.SaveChangesAsync(ct);

        return Results.Ok(new { account.Id, account.ReservedMargin, account.AvailableMargin });
    }

    private static async Task<IResult> ReleaseMarginAsync(
        Guid accountId,
        decimal amount,
        PortfolioDbContext db,
        CancellationToken ct)
    {
        var account = await db.Accounts.FindAsync([accountId], ct);
        if (account == null) return Results.NotFound();

        account.ReservedMargin = Math.Max(0m, account.ReservedMargin - amount);
        await db.SaveChangesAsync(ct);

        return Results.Ok(new { account.Id, account.ReservedMargin, account.AvailableMargin });
    }
}
