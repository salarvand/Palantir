using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Palantir.AiAssistantService.Agents;
using Palantir.Platform.Endpoints;

namespace Palantir.AiAssistantService.Protocols;

public record AgUiStreamRequest(
    Guid ClientId,
    string Symbol,
    string Side,
    decimal Quantity,
    decimal Price
);

public class AgUiStreamEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/ai/stream", async (
            AgUiStreamRequest request,
            TradeCopilotCoordinator coordinator,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            httpContext.Response.ContentType = "text/event-stream";
            httpContext.Response.Headers.CacheControl = "no-cache";
            httpContext.Response.Headers.Connection = "keep-alive";

            await SendSseEventAsync(httpContext, "thought_start", new { stage = "gathering_market_data", symbol = request.Symbol }, ct);

            // Simulated real-time reasoning tokens
            await Task.Delay(50, ct);
            await SendSseEventAsync(httpContext, "reasoning_token", new { delta = $"Analyzing depth and L2 order book for {request.Symbol}..." }, ct);

            await Task.Delay(50, ct);
            await SendSseEventAsync(httpContext, "reasoning_token", new { delta = "Evaluating pre-trade margin bounds and concentration limits..." }, ct);

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
                ct: ct
            );

            await SendSseEventAsync(httpContext, "proposal_card", proposal.ProposalCard, ct);
            await SendSseEventAsync(httpContext, "risk_gauge", proposal.RiskGauge, ct);
            await SendSseEventAsync(httpContext, "done", new { summary = proposal.Summary }, ct);
        })
        .WithName("AiStreamEvents")
        .WithTags("AI Assistant")
        .RequireAuthorization();
    }

    private static async Task SendSseEventAsync(HttpContext context, string eventType, object data, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(data);
        var message = $"event: {eventType}\ndata: {json}\n\n";
        await context.Response.WriteAsync(message, ct);
        await context.Response.Body.FlushAsync(ct);
    }
}
