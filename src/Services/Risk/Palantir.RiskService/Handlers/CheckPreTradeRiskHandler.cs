using System.Text.Json;
using Palantir.Platform.Observability;
using Palantir.RiskService.Engine;
using Palantir.RiskService.IntegrationEvents.Events;
using Palantir.SharedKernel;
using Wolverine;

namespace Palantir.RiskService.Handlers;

public sealed class CheckPreTradeRiskHandler
{
    private const decimal DefaultAvailableMargin = 100_000m;
    private readonly RiskEngine _riskEngine;
    private readonly ILogger<CheckPreTradeRiskHandler> _logger;

    public CheckPreTradeRiskHandler(RiskEngine riskEngine, ILogger<CheckPreTradeRiskHandler> logger)
    {
        _riskEngine = riskEngine;
        _logger = logger;
    }

    public async Task Handle(CheckPreTradeRiskCommand command, IMessageBus bus, CancellationToken ct)
    {
        var evaluation = _riskEngine.EvaluatePreTradeRisk(new RiskEvaluationRequest(
            command.ClientId,
            command.Symbol,
            TradeSide.Buy,
            command.Quantity,
            command.Price > 0 ? command.Price : 100m,
            DefaultAvailableMargin));

        var traceId = TraceContextSnapshot.GetCurrentTraceId();

        if (evaluation.Approved)
        {
            await bus.PublishAsync(new PreTradeRiskApprovedEvent(command.OrderId, evaluation.RequiredMargin));
            await bus.PublishAsync(CreateAuditEvent("PreTradeRiskApproved", command, evaluation, traceId));
        }
        else
        {
            var reason = evaluation.RejectionReason ?? "Pre-trade risk limit exceeded.";
            _logger.LogWarning("Order {OrderId} rejected by RiskEngine: {Reason}", command.OrderId, reason);
            await bus.PublishAsync(new OrderSettlementFailedEvent(command.OrderId, reason));
            await bus.PublishAsync(CreateAuditEvent("PreTradeRiskRejected", command, evaluation, traceId));
        }
    }

    private static AuditTrailEvent CreateAuditEvent(
        string eventType,
        CheckPreTradeRiskCommand command,
        RiskEvaluationResult evaluation,
        string? traceId) =>
        new(
            eventType,
            command.OrderId,
            null,
            command.ClientId,
            command.Symbol,
            command.Price,
            command.Quantity,
            JsonSerializer.Serialize(evaluation),
            DateTimeOffset.UtcNow,
            traceId);
}
