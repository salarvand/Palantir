namespace Palantir.AiAssistantService.AiProvider;

public sealed class AiProviderOptions
{
    public const string SectionName = "AiProvider";

    public string Endpoint { get; set; } = "https://api.openai.com/v1/";
    public string ApiKey { get; set; } = string.Empty;
    public string ChatModel { get; set; } = "gpt-4o-mini";
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";
    public int EmbeddingDimensions { get; set; } = 768;
}
