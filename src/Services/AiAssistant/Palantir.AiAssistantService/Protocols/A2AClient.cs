using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace Palantir.AiAssistantService.Protocols;

public record A2AMessage(
    string SenderAgentId,
    string TargetAgentId,
    string Action,
    Dictionary<string, object> Payload,
    DateTimeOffset TimestampUtc
);

public record A2AResponse(
    bool Success,
    string Message,
    Dictionary<string, object>? Result
);

public interface IA2AClient
{
    Task<A2AResponse> DispatchAsync(A2AMessage message, CancellationToken ct = default);
}

public class A2AClient : IA2AClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<A2AClient> _logger;

    public A2AClient(HttpClient httpClient, ILogger<A2AClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<A2AResponse> DispatchAsync(A2AMessage message, CancellationToken ct = default)
    {
        _logger.LogInformation("A2A protocol dispatch: {Sender} -> {Target} for {Action}",
            message.SenderAgentId, message.TargetAgentId, message.Action);

        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/v1/a2a/messages", message, ct);
            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadFromJsonAsync<A2AResponse>(cancellationToken: ct);
                return body ?? new A2AResponse(true, "A2A dispatch acknowledged.", null);
            }

            return new A2AResponse(false, $"A2A remote agent returned {response.StatusCode}", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "A2A dispatch failed for target {Target}", message.TargetAgentId);
            return new A2AResponse(false, ex.Message, null);
        }
    }
}
