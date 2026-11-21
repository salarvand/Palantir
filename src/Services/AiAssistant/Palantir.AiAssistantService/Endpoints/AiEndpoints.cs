using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Palantir.AiAssistantService.Mcp;
using Palantir.Platform.Endpoints;

namespace Palantir.AiAssistantService.Endpoints;

public class AiEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/mcp")
            .WithTags("Model Context Protocol (MCP)");

        group.MapGet("/tools", () => Results.Ok(McpToolRegistry.AvailableTools));

        group.MapPost("/tools/call", (McpToolCallRequest request) =>
        {
            var result = McpToolRegistry.ExecuteTool(request);
            return result.IsSuccess ? Results.Ok(result) : Results.BadRequest(result);
        });
    }
}
