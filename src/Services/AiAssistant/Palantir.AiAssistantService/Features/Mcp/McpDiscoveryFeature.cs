using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Palantir.AiAssistantService.Governance;
using Palantir.AiAssistantService.Mcp;
using Palantir.Platform.Endpoints;

namespace Palantir.AiAssistantService.Features.Mcp;

internal sealed class McpDiscoveryEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/ai/mcp/tools", (IMcpToolExecutor executor) => TypedResults.Ok(executor.AvailableTools))
            .ProducesGet<IReadOnlyList<McpToolDefinition>>()
            .WithTags("Model Context Protocol (MCP)")
            .WithName(nameof(McpDiscoveryEndpoint))
            .MapToApiVersion(ApiVersioningExtensions.V1)
            .RequireAuthorization();

        app.MapPost("/ai/mcp/tools/call", async (
                McpToolCallRequest request,
                IMcpToolExecutor executor,
                AgentGovernanceManager governance,
                CancellationToken ct) =>
            {
                if (!governance.IsToolAllowed("trade-copilot-coordinator", request.ToolName, out var policyError))
                {
                    return Results.Problem(policyError, statusCode: StatusCodes.Status403Forbidden);
                }

                if (!governance.TryAcquireExecutionBudget(out var budgetError))
                {
                    return Results.Problem(budgetError, statusCode: StatusCodes.Status429TooManyRequests);
                }

                var result = await executor.ExecuteToolAsync(request, ct);
                return result.IsSuccess ? TypedResults.Ok(result) : TypedResults.BadRequest(result);
            })
            .ProducesPostWithoutLocation<McpToolCallResult>()
            .WithTags("Model Context Protocol (MCP)")
            .WithName("InvokeMcpTool")
            .MapToApiVersion(ApiVersioningExtensions.V1)
            .RequireAuthorization();
    }
}
