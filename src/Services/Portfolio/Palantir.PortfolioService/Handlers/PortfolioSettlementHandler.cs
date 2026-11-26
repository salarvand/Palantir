using Microsoft.EntityFrameworkCore;
using Palantir.PortfolioService.Infrastructure;
using Palantir.PortfolioService.IntegrationEvents.Events;

namespace Palantir.PortfolioService.Handlers;

public sealed class PortfolioSettlementHandler
{
    private readonly PortfolioDbContext _db;
    private readonly ILogger<PortfolioSettlementHandler> _logger;

    public PortfolioSettlementHandler(PortfolioDbContext db, ILogger<PortfolioSettlementHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Handle(ReleaseReservedMarginCommand command, CancellationToken ct)
    {
        var account = await _db.Accounts.FirstOrDefaultAsync(a => a.Id == command.ClientId || a.UserId == command.ClientId, ct);
        if (account == null)
        {
            _logger.LogWarning("Account not found for margin release on Order {OrderId}, Client {ClientId}.", command.OrderId, command.ClientId);
            return;
        }

        account.ReservedMargin = Math.Max(0m, account.ReservedMargin - command.Amount);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Released {Amount} reserved margin for Client {ClientId} (Order {OrderId}): {Reason}",
            command.Amount, command.ClientId, command.OrderId, command.Reason);
    }

    public async Task Handle(ConfirmSettlementEvent evt, CancellationToken ct)
    {
        _logger.LogInformation("Settlement confirmed for Order {OrderId} at {SettledAtUtc}.", evt.OrderId, evt.SettledAtUtc);
        await Task.CompletedTask;
    }

    public async Task Handle(PreTradeRiskApprovedEvent evt, CancellationToken ct)
    {
        _logger.LogDebug("Pre-trade risk approved for Order {OrderId} requiring margin {Margin}.", evt.OrderId, evt.RequiredMargin);
        await Task.CompletedTask;
    }
}
