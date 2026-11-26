using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Palantir.AiAssistantService.Governance;

public interface IPiiRedactor
{
    string Redact(string input);
}

public sealed class PresidioPiiRedactor : IPiiRedactor
{
    private static readonly Regex EmailRegex = new(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", RegexOptions.Compiled);
    private static readonly Regex PhoneRegex = new(@"\b(\+?[0-9]{1,3}[-.\s]?)?\(?[0-9]{3}\)?[-.\s]?[0-9]{3}[-.\s]?[0-9]{4}\b", RegexOptions.Compiled);
    private static readonly Regex SsnRegex = new(@"\b\d{3}-\d{2}-\d{4}\b", RegexOptions.Compiled);
    private static readonly Regex CreditCardRegex = new(@"\b(?:\d{4}[-\s]?){3}\d{4}\b", RegexOptions.Compiled);

    public string Redact(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        var result = EmailRegex.Replace(input, "<REDACTED_EMAIL>");
        result = PhoneRegex.Replace(result, "<REDACTED_PHONE>");
        result = SsnRegex.Replace(result, "<REDACTED_SSN>");
        result = CreditCardRegex.Replace(result, "<REDACTED_CARD>");
        return result;
    }
}

public sealed class AgentGovernancePolicy
{
    public string DefaultAction { get; set; } = "deny";
    public int MaxDailyExecutions { get; set; } = 50000;
    public List<AgentPolicyRule> AllowedAgents { get; set; } = [];
    public List<string> BlockedPromptPatterns { get; set; } = [];
}

public sealed class AgentPolicyRule
{
    public string Id { get; set; } = string.Empty;
    public List<string> AllowedTools { get; set; } = [];
}

public sealed class AgentGovernanceManager
{
    private readonly IPiiRedactor _redactor;
    private readonly ILogger<AgentGovernanceManager> _logger;
    private readonly AgentGovernancePolicy _policy;
    private readonly List<Regex> _blockedRegexes;
    private int _dailyExecutionCount;

    public AgentGovernanceManager(IPiiRedactor redactor, ILogger<AgentGovernanceManager> logger)
    {
        _redactor = redactor;
        _logger = logger;
        _policy = LoadPolicy(logger);
        _blockedRegexes = _policy.BlockedPromptPatterns
            .Select(p => new Regex(p, RegexOptions.IgnoreCase | RegexOptions.Compiled))
            .ToList();
    }

    public bool TryAcquireExecutionBudget(out string? rejectionReason)
    {
        if (Interlocked.Increment(ref _dailyExecutionCount) > _policy.MaxDailyExecutions)
        {
            rejectionReason = $"Agent daily execution budget ({_policy.MaxDailyExecutions}) exhausted.";
            _logger.LogWarning("Execution rejected: budget exceeded.");
            return false;
        }

        rejectionReason = null;
        return true;
    }

    public bool IsToolAllowed(string agentId, string toolName, out string? rejectionReason)
    {
        var rule = _policy.AllowedAgents.FirstOrDefault(a => string.Equals(a.Id, agentId, StringComparison.OrdinalIgnoreCase));
        if (rule is null)
        {
            if (string.Equals(_policy.DefaultAction, "allow", StringComparison.OrdinalIgnoreCase))
            {
                rejectionReason = null;
                return true;
            }

            rejectionReason = $"Agent '{agentId}' is not authorized by governance policy.";
            _logger.LogWarning("Policy violation: {Reason}", rejectionReason);
            return false;
        }

        if (!rule.AllowedTools.Contains(toolName, StringComparer.OrdinalIgnoreCase))
        {
            rejectionReason = $"Tool '{toolName}' is denied for agent '{agentId}' by governance policy.";
            _logger.LogWarning("Policy violation: {Reason}", rejectionReason);
            return false;
        }

        rejectionReason = null;
        return true;
    }

    public bool ValidatePromptSafety(string rawPrompt, out string? rejectionReason)
    {
        foreach (var regex in _blockedRegexes)
        {
            if (regex.IsMatch(rawPrompt))
            {
                rejectionReason = "Prompt blocked by AI governance safety policy.";
                _logger.LogWarning("Prompt safety violation matched pattern {Pattern}", regex);
                return false;
            }
        }

        rejectionReason = null;
        return true;
    }

    public string SanitizePrompt(string rawPrompt) => _redactor.Redact(rawPrompt);

    private static AgentGovernancePolicy LoadPolicy(ILogger logger)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Policies", "ai-agents.yaml");
        if (!File.Exists(path))
        {
            logger.LogWarning("AI governance policy YAML not found at {Path}; using strict default policy.", path);
            return new AgentGovernancePolicy();
        }

        var yaml = File.ReadAllText(path);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        return deserializer.Deserialize<AgentGovernancePolicy>(yaml) ?? new AgentGovernancePolicy();
    }
}
