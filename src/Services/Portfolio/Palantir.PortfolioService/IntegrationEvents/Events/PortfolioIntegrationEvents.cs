using Wolverine.Attributes;

namespace Palantir.PortfolioService.IntegrationEvents.Events;

[MessageIdentity("palantir.release-reserved-margin")]
public sealed record ReleaseReservedMarginCommand(
    Guid OrderId,
    Guid ClientId,
    decimal Amount,
    string Reason);

[MessageIdentity("palantir.confirm-settlement")]
public sealed record ConfirmSettlementEvent(
    Guid OrderId,
    DateTimeOffset SettledAtUtc);

[MessageIdentity("palantir.pre-trade-risk-approved")]
public sealed record PreTradeRiskApprovedEvent(
    Guid OrderId,
    decimal RequiredMargin);
