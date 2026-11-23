using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Palantir.AiAssistantService.Governance;

public interface IPiiRedactor
{
    string Redact(string input);
}

public class PresidioPiiRedactor : IPiiRedactor
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

public class AgentGovernanceManager
{
    private readonly IPiiRedactor _redactor;
    private readonly ILogger<AgentGovernanceManager> _logger;
    private int _dailyExecutionCount;
    private const int MaxDailyExecutions = 50000;

    public AgentGovernanceManager(IPiiRedactor redactor, ILogger<AgentGovernanceManager> logger)
    {
        _redactor = redactor;
        _logger = logger;
    }

    public bool TryAcquireExecutionBudget(out string? rejectionReason)
    {
        if (Interlocked.Increment(ref _dailyExecutionCount) > MaxDailyExecutions)
        {
            rejectionReason = "Agent daily execution budget exhausted.";
            _logger.LogWarning("Execution rejected: budget exceeded.");
            return false;
        }

        rejectionReason = null;
        return true;
    }

    public string SanitizePrompt(string rawPrompt) => _redactor.Redact(rawPrompt);
}
