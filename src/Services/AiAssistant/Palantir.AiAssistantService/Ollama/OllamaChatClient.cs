using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;

namespace Palantir.AiAssistantService.Ollama;

public sealed class OllamaChatClient : IChatClient
{
    private readonly HttpClient _httpClient;
    private readonly string _defaultModel;

    public ChatClientMetadata Metadata { get; }

    public OllamaChatClient(HttpClient httpClient, string defaultModel = "llama3.2")
    {
        _httpClient = httpClient;
        _defaultModel = defaultModel;
        Metadata = new ChatClientMetadata("Ollama", new Uri(httpClient.BaseAddress?.ToString() ?? "http://localhost:11434"), defaultModel);
    }

    private sealed class OllamaChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<OllamaChatMessage> Messages { get; set; } = [];

        [JsonPropertyName("stream")]
        public bool Stream { get; set; }
    }

    private sealed class OllamaChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private sealed class OllamaChatResponse
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public OllamaChatMessage? Message { get; set; }

        [JsonPropertyName("done")]
        public bool Done { get; set; }
    }

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var model = options?.ModelId ?? _defaultModel;
        var requestPayload = new OllamaChatRequest
        {
            Model = model,
            Stream = false,
            Messages = chatMessages.Select(m => new OllamaChatMessage
            {
                Role = m.Role.Value,
                Content = m.Text ?? string.Empty
            }).ToList()
        };

        var response = await _httpClient.PostAsJsonAsync("/api/chat", requestPayload, cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(cancellationToken: cancellationToken);
        var content = body?.Message?.Content ?? string.Empty;

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
        var model = options?.ModelId ?? _defaultModel;
        var requestPayload = new OllamaChatRequest
        {
            Model = model,
            Stream = true,
            Messages = chatMessages.Select(m => new OllamaChatMessage
            {
                Role = m.Role.Value,
                Content = m.Text ?? string.Empty
            }).ToList()
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/chat")
        {
            Content = JsonContent.Create(requestPayload)
        };

        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line)) continue;

            OllamaChatResponse? chunk = null;
            try
            {
                chunk = JsonSerializer.Deserialize<OllamaChatResponse>(line);
            }
            catch { /* Skip malformed streaming lines */ }

            if (chunk?.Message?.Content != null)
            {
                yield return new ChatResponseUpdate(ChatRole.Assistant, chunk.Message.Content);
            }
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceType.IsInstanceOfType(this) ? this : null;

    public void Dispose() { }
}

public sealed class OllamaEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    private readonly HttpClient _httpClient;
    private readonly string _defaultModel;

    public EmbeddingGeneratorMetadata Metadata { get; }

    public OllamaEmbeddingGenerator(HttpClient httpClient, string defaultModel = "nomic-embed-text")
    {
        _httpClient = httpClient;
        _defaultModel = defaultModel;
        Metadata = new EmbeddingGeneratorMetadata("Ollama", new Uri(httpClient.BaseAddress?.ToString() ?? "http://localhost:11434"), defaultModel);
    }

    private sealed class OllamaEmbeddingRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = string.Empty;
    }

    private sealed class OllamaEmbeddingResponse
    {
        [JsonPropertyName("embedding")]
        public float[]? Embedding { get; set; }
    }

    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var model = options?.ModelId ?? _defaultModel;
        var results = new List<Embedding<float>>();

        foreach (var value in values)
        {
            var request = new OllamaEmbeddingRequest
            {
                Model = model,
                Prompt = value
            };

            var response = await _httpClient.PostAsJsonAsync("/api/embeddings", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>(cancellationToken: cancellationToken);
            var vector = body?.Embedding ?? [];
            results.Add(new Embedding<float>(new ReadOnlyMemory<float>(vector)));
        }

        return new GeneratedEmbeddings<Embedding<float>>(results);
    }

    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceType.IsInstanceOfType(this) ? this : null;

    public void Dispose() { }
}
