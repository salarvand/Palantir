using System.Text.Json;
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

        // 1. Standard MCP JSON-RPC 2.0 protocol endpoint
        group.MapPost("/", HandleMcpRpcAsync);

        // 2. Direct REST convenience endpoints
        group.MapGet("/tools", (IMcpToolExecutor executor) => Results.Ok(executor.AvailableTools));

        group.MapPost("/tools/call", async (McpToolCallRequest request, IMcpToolExecutor executor, CancellationToken ct) =>
        {
            var result = await executor.ExecuteToolAsync(request, ct);
            return result.IsSuccess ? Results.Ok(result) : Results.BadRequest(result);
        });
    }

    private static async Task<IResult> HandleMcpRpcAsync(
        JsonRpcRequest rpcRequest,
        IMcpToolExecutor executor,
        CancellationToken ct)
    {
        var method = rpcRequest.Method?.ToLowerInvariant();
        var id = rpcRequest.Id;

        switch (method)
        {
            case "initialize":
                return Results.Ok(new JsonRpcResponse(
                    "2.0",
                    id,
                    new
                    {
                        protocolVersion = "2024-11-05",
                        capabilities = new
                        {
                            tools = new { }
                        },
                        serverInfo = new
                        {
                            name = "palantir-ai-assistant",
                            version = "1.0.0"
                        }
                    }
                ));

            case "notifications/initialized":
            case "ping":
                return Results.Ok(new JsonRpcResponse("2.0", id, new { }));

            case "tools/list":
                return Results.Ok(new JsonRpcResponse(
                    "2.0",
                    id,
                    new
                    {
                        tools = executor.AvailableTools
                    }
                ));

            case "tools/call":
                if (rpcRequest.Params == null)
                {
                    return Results.Ok(new JsonRpcResponse(
                        "2.0",
                        id,
                        null,
                        new JsonRpcError(-32602, "Invalid params: missing parameters object.")));
                }

                string? toolName = null;
                Dictionary<string, object>? arguments = null;

                if (rpcRequest.Params.Value.ValueKind == JsonValueKind.Object)
                {
                    if (rpcRequest.Params.Value.TryGetProperty("name", out var nameProp))
                    {
                        toolName = nameProp.GetString();
                    }

                    if (rpcRequest.Params.Value.TryGetProperty("arguments", out var argsProp) && argsProp.ValueKind == JsonValueKind.Object)
                    {
                        arguments = JsonSerializer.Deserialize<Dictionary<string, object>>(argsProp.GetRawText());
                    }
                }

                if (string.IsNullOrWhiteSpace(toolName))
                {
                    return Results.Ok(new JsonRpcResponse(
                        "2.0",
                        id,
                        null,
                        new JsonRpcError(-32602, "Invalid params: 'name' is required.")));
                }

                var toolResult = await executor.ExecuteToolAsync(new McpToolCallRequest(toolName, arguments), ct);

                return Results.Ok(new JsonRpcResponse(
                    "2.0",
                    id,
                    new
                    {
                        content = new[]
                        {
                            new
                            {
                                type = "text",
                                text = JsonSerializer.Serialize(toolResult.Result ?? toolResult.ErrorMessage)
                            }
                        },
                        isError = !toolResult.IsSuccess
                    }
                ));

            default:
                return Results.Ok(new JsonRpcResponse(
                    "2.0",
                    id,
                    null,
                    new JsonRpcError(-32601, $"Method '{rpcRequest.Method}' not found.")));
        }
    }
}
