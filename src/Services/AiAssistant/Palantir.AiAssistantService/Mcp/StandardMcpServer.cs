using System.Text.Json;
using Palantir.AiAssistantService.Governance;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace Palantir.AiAssistantService.Mcp;

public class StandardMcpServer
{
    private readonly IMcpToolExecutor _executor;
    private readonly AgentGovernanceManager _governance;
    private readonly ILogger<StandardMcpServer> _logger;

    public StandardMcpServer(
        IMcpToolExecutor executor,
        AgentGovernanceManager governance,
        ILogger<StandardMcpServer> logger)
    {
        _executor = executor;
        _governance = governance;
        _logger = logger;
    }

    public async Task<McpToolCallResult> CallToolAsync(string toolName, Dictionary<string, object>? arguments, CancellationToken ct = default)
    {
        if (!_governance.TryAcquireExecutionBudget(out var rejectionReason))
        {
            return new McpToolCallResult(toolName, false, null!, rejectionReason);
        }

        var request = new McpToolCallRequest(toolName, arguments);
        return await _executor.ExecuteToolAsync(request, ct);
    }
}
