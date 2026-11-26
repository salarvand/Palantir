using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace Palantir.AiAssistantService.AiProvider;

internal static class AiProviderHttpGuard
{
    public static void EnsureApiKeyConfigured(AiProviderOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new InvalidOperationException(
                "AiProvider:ApiKey is not configured. Set it via User Secrets: dotnet user-secrets set \"AiProvider:ApiKey\" \"<your-key>\".");
        }
    }

    public static void ConfigureClient(HttpClient client, AiProviderOptions options)
    {
        var baseUrl = string.IsNullOrWhiteSpace(options.Endpoint)
            ? "https://api.openai.com/v1/"
            : options.Endpoint.TrimEnd('/') + "/";

        client.BaseAddress = new Uri(baseUrl);
        if (!string.IsNullOrWhiteSpace(options.ApiKey))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        }
    }
}

public sealed class ApiKeyChatClient : IChatClient
{
    private readonly HttpClient _httpClient;
    private readonly AiProviderOptions _options;

    public ChatClientMetadata Metadata { get; }

    public ApiKeyChatClient(HttpClient httpClient, IOptions<AiProviderOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
        Metadata = new ChatClientMetadata("OpenAiCompatible", httpClient.BaseAddress, _options.ChatModel);
    }

    private sealed class ChatCompletionRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<ChatMessagePayload> Messages { get; set; } = [];

        [JsonPropertyName("stream")]
        public bool Stream { get; set; }
    }

    private sealed class ChatMessagePayload
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private sealed class ChatCompletionResponse
    {
        [JsonPropertyName("choices")]
        public List<ChatChoice>? Choices { get; set; }
    }

    private sealed class ChatChoice
    {
        [JsonPropertyName("message")]
        public ChatMessagePayload? Message { get; set; }

        [JsonPropertyName("delta")]
        public ChatMessagePayload? Delta { get; set; }
    }

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        AiProviderHttpGuard.EnsureApiKeyConfigured(_options);

        var model = options?.ModelId ?? _options.ChatModel;
        var payload = BuildRequest(model, chatMessages, stream: false);

        var response = await _httpClient.PostAsJsonAsync("chat/completions", payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(cancellationToken: cancellationToken);
        var content = body?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;

        return new ChatResponse(new ChatMessage(ChatRole.Assistant, content))
        {
            ModelId = model
        };
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        AiProviderHttpGuard.EnsureApiKeyConfigured(_options);

        var model = options?.ModelId ?? _options.ChatModel;
        var payload = BuildRequest(model, chatMessages, stream: true);

        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = JsonContent.Create(payload)
        };

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var data = line["data:".Length..].Trim();
            if (string.Equals(data, "[DONE]", StringComparison.OrdinalIgnoreCase))
            {
                yield break;
            }

            ChatCompletionResponse? chunk = null;
            try
            {
                chunk = JsonSerializer.Deserialize<ChatCompletionResponse>(data);
            }
            catch
            {
                // Skip malformed SSE chunks
            }

            var deltaText = chunk?.Choices?.FirstOrDefault()?.Delta?.Content;
            if (!string.IsNullOrEmpty(deltaText))
            {
                yield return new ChatResponseUpdate(ChatRole.Assistant, deltaText);
            }
        }
    }

    private static ChatCompletionRequest BuildRequest(string model, IEnumerable<ChatMessage> chatMessages, bool stream) =>
        new()
        {
            Model = model,
            Stream = stream,
            Messages = chatMessages.Select(m => new ChatMessagePayload
            {
                Role = m.Role.Value,
                Content = m.Text ?? string.Empty
            }).ToList()
        };

    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceType.IsInstanceOfType(this) ? this : null;

    public void Dispose() { }
}

public sealed class ApiKeyEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    private readonly HttpClient _httpClient;
    private readonly AiProviderOptions _options;

    public EmbeddingGeneratorMetadata Metadata { get; }

    public ApiKeyEmbeddingGenerator(HttpClient httpClient, IOptions<AiProviderOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
        Metadata = new EmbeddingGeneratorMetadata("OpenAiCompatible", httpClient.BaseAddress, _options.EmbeddingModel);
    }

    private sealed class EmbeddingsRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("input")]
        public List<string> Input { get; set; } = [];

        [JsonPropertyName("dimensions")]
        public int Dimensions { get; set; }
    }

    private sealed class EmbeddingsResponse
    {
        [JsonPropertyName("data")]
        public List<EmbeddingItem>? Data { get; set; }
    }

    private sealed class EmbeddingItem
    {
        [JsonPropertyName("embedding")]
        public float[]? Embedding { get; set; }
    }

    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        AiProviderHttpGuard.EnsureApiKeyConfigured(_options);

        var model = options?.ModelId ?? _options.EmbeddingModel;
        var request = new EmbeddingsRequest
        {
            Model = model,
            Input = values.ToList(),
            Dimensions = options?.Dimensions ?? _options.EmbeddingDimensions
        };

        var response = await _httpClient.PostAsJsonAsync("embeddings", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<EmbeddingsResponse>(cancellationToken: cancellationToken);
        var embeddings = (body?.Data ?? [])
            .Select(d => new Embedding<float>(new ReadOnlyMemory<float>(d.Embedding ?? [])))
            .ToList();

        return new GeneratedEmbeddings<Embedding<float>>(embeddings);
    }

    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceType.IsInstanceOfType(this) ? this : null;

    public void Dispose() { }
}
