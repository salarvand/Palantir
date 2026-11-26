using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Palantir.AiAssistantService.Agents;
using Palantir.AiAssistantService.Governance;
using Palantir.Platform.Endpoints;

namespace Palantir.AiAssistantService.Features.AgUi;

public sealed record AgUiStreamRequest(
    Guid ClientId,
    string Symbol,
    string Side,
    decimal Quantity,
    decimal Price,
    string? UserPrompt = null);

internal sealed class AgUiStreamEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/ai/stream", HandleStreamAsync)
            .WithName("AiStreamEvents")
            .WithTags("AG-UI & A2UI")
            .MapToApiVersion(ApiVersioningExtensions.V1)
            .RequireAuthorization();
    }

    private static async Task HandleStreamAsync(
        AgUiStreamRequest request,
        TradeCopilotCoordinator coordinator,
        AgentGovernanceManager governance,
        HttpContext httpContext,
        CancellationToken ct)
    {
        if (!governance.TryAcquireExecutionBudget(out var budgetError))
        {
            httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await httpContext.Response.WriteAsJsonAsync(new { error = budgetError }, ct);
            return;
        }

        var prompt = governance.SanitizePrompt(request.UserPrompt ?? $"Evaluate {request.Side} {request.Quantity} {request.Symbol} @ {request.Price}");
        if (!governance.ValidatePromptSafety(prompt, out var safetyError))
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await httpContext.Response.WriteAsJsonAsync(new { error = safetyError }, ct);
            return;
        }

        httpContext.Response.ContentType = "text/event-stream";
        httpContext.Response.Headers.CacheControl = "no-cache";
        httpContext.Response.Headers.Connection = "keep-alive";

        var runId = Guid.NewGuid().ToString("N");
        await SendSseEventAsync(httpContext, "RUN_STARTED", new { runId, symbol = request.Symbol }, ct);
        await SendSseEventAsync(httpContext, "thought_start", new { stage = "multi_agent_orchestration", symbol = request.Symbol }, ct);

        var proposal = await coordinator.GenerateProposalAsync(
            request.ClientId,
            request.Symbol,
            request.Side,
            request.Quantity,
            request.Price,
            bestBid: request.Price * 0.999m,
            bestAsk: request.Price * 1.001m,
            bidVolume: 500m,
            askVolume: 350m,
            currentMarginUsed: 12000m,
            totalAccountEquity: 50000m,
            ct: ct);

        await SendSseEventAsync(httpContext, "reasoning_token", new { delta = proposal.Analysis.Rationale }, ct);
        await SendSseEventAsync(httpContext, "a2ui_component", new { component = "TradeProposalCard", payload = proposal.ProposalCard }, ct);
        await SendSseEventAsync(httpContext, "a2ui_component", new { component = "RiskGauge", payload = proposal.RiskGauge }, ct);
        await SendSseEventAsync(httpContext, "RUN_FINISHED", new { runId, summary = proposal.Summary }, ct);
    }

    private static async Task SendSseEventAsync(HttpContext context, string eventType, object data, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(data);
        await context.Response.WriteAsync($"event: {eventType}\ndata: {json}\n\n", ct);
        await context.Response.Body.FlushAsync(ct);
    }
}
