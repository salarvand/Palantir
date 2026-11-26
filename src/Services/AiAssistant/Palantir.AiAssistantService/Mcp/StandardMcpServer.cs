using Palantir.AiAssistantService.Governance;

namespace Palantir.AiAssistantService.Mcp;

public class StandardMcpServer
{
    private readonly IMcpToolExecutor _executor;
    private readonly AgentGovernanceManager _governance;

    public StandardMcpServer(
        IMcpToolExecutor executor,
        AgentGovernanceManager governance)
    {
        _executor = executor;
        _governance = governance;
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
