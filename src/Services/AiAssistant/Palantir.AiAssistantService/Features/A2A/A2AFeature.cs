using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Palantir.AiAssistantService.Agents;
using Palantir.AiAssistantService.Governance;
using Palantir.AiAssistantService.Protocols;
using Palantir.Platform.Endpoints;

namespace Palantir.AiAssistantService.Features.A2A;

public sealed record AgentSkillDescriptor(string Id, string Name, string Description, IReadOnlyList<string> Tags);

public sealed record AgentCardDescriptor(
    string Name,
    string Description,
    string Version,
    IReadOnlyList<string> DefaultInputModes,
    IReadOnlyList<string> DefaultOutputModes,
    IReadOnlyList<AgentSkillDescriptor> Skills);

internal sealed class A2AEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/a2a/card", () => TypedResults.Ok(new AgentCardDescriptor(
                "PalantirTradeCopilot",
                "Multi-agent trading analysis, pre-trade margin risk evaluation, and A2UI advisory coordinator.",
                "1.0.0",
                ["text", "application/json"],
                ["text", "application/vnd.palantir.a2ui+json"],
                [
                    new AgentSkillDescriptor("trade_analysis", "TradeAnalysisAgent", "Evaluates L2 order book depth, spread, and imbalance momentum.", ["trading", "orderbook"]),
                    new AgentSkillDescriptor("risk_guard", "RiskGuardAgent", "Validates projected margin utilization and single-order concentration limits.", ["risk", "compliance"])
                ])))
            .ProducesGet<AgentCardDescriptor>()
            .WithTags("Agent-to-Agent (A2A)")
            .WithName("GetA2AAgentCard")
            .MapToApiVersion(ApiVersioningExtensions.V1);

        app.MapPost("/a2a/messages", async (
                A2AMessage message,
                TradeCopilotCoordinator coordinator,
                AgentGovernanceManager governance,
                CancellationToken ct) =>
            {
                if (!governance.TryAcquireExecutionBudget(out var budgetError))
                {
                    return TypedResults.Ok(new A2AResponse(false, budgetError ?? "Budget exceeded", null));
                }

                var symbol = message.Payload.TryGetValue("symbol", out var s) ? s?.ToString() ?? "PAL/USD" : "PAL/USD";
                var side = message.Payload.TryGetValue("side", out var sd) ? sd?.ToString() ?? "BUY" : "BUY";
                var qty = message.Payload.TryGetValue("quantity", out var q) && decimal.TryParse(q?.ToString(), out var qv) ? qv : 100m;
                var price = message.Payload.TryGetValue("price", out var p) && decimal.TryParse(p?.ToString(), out var pv) ? pv : 150m;

                var proposal = await coordinator.GenerateProposalAsync(
                    Guid.Empty,
                    symbol,
                    side,
                    qty,
                    price,
                    price * 0.999m,
                    price * 1.001m,
                    500m,
                    350m,
                    12000m,
                    50000m,
                    ct);

                return TypedResults.Ok(new A2AResponse(
                    true,
                    proposal.Summary,
                    new Dictionary<string, object>
                    {
                        ["proposalCard"] = proposal.ProposalCard,
                        ["riskGauge"] = proposal.RiskGauge
                    }));
            })
            .ProducesPostWithoutLocation<A2AResponse>()
            .WithTags("Agent-to-Agent (A2A)")
            .WithName("DispatchA2AMessage")
            .MapToApiVersion(ApiVersioningExtensions.V1)
            .RequireAuthorization();
    }
}
