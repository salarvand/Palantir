using Palantir.AiAssistantService.Mcp;
using Palantir.Constants;
using Palantir.Platform.Caching;
using Palantir.Platform.Endpoints;
using Palantir.Platform.Exceptions;
using Palantir.Platform.Security;
using Palantir.ServiceDefaults;
using Qdrant.Client;
using Qdrant.Client.Grpc;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var redisConnectionString = builder.Configuration.GetConnectionString(AspireResourceNames.Redis)
                            ?? "localhost:6379";

builder.Services.AddPalantirCaching(redisConnectionString);
builder.Services.AddPalantirSecurity();
builder.Services.AddPalantirProblemDetails();

// Qdrant Vector Client
var qdrantEndpoint = builder.Configuration.GetConnectionString(AspireResourceNames.Qdrant)
    ?? builder.Configuration["Qdrant:Endpoint"]
    ?? builder.Configuration["Qdrant__Endpoint"]
    ?? "http://localhost:6334";

var qdrantUri = new Uri(qdrantEndpoint);
var qdrantClient = new QdrantClient(
    qdrantUri.Host,
    qdrantUri.Port > 0 ? qdrantUri.Port : 6334,
    https: qdrantUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase));

builder.Services.AddSingleton(qdrantClient);
builder.Services.AddHostedService<QdrantCollectionInitializer>();

// Downstream Microservice Clients for MCP Tools
builder.Services.AddHttpClient("oms", client =>
{
    client.BaseAddress = new Uri($"http://{AspireResourceNames.OmsService}");
});

builder.Services.AddHttpClient("market-data", client =>
{
    client.BaseAddress = new Uri($"http://{AspireResourceNames.MarketDataService}");
});

builder.Services.AddHttpClient("risk", client =>
{
    client.BaseAddress = new Uri($"http://{AspireResourceNames.RiskService}");
});

builder.Services.AddHttpClient("portfolio", client =>
{
    client.BaseAddress = new Uri($"http://{AspireResourceNames.PortfolioService}");
});

// MCP Tools Executor
builder.Services.AddScoped<IMcpToolExecutor, McpToolExecutor>();
builder.Services.AddScoped<Palantir.AiAssistantService.Mcp.StandardMcpServer>();

// Ollama Local AI & Embedding Wiring (Zero Azure)
var ollamaEndpoint = builder.Configuration["Ollama__Endpoint"]
    ?? builder.Configuration.GetConnectionString(AspireResourceNames.Ollama)
    ?? "http://localhost:11434";

builder.Services.AddHttpClient<Microsoft.Extensions.AI.IChatClient, Palantir.AiAssistantService.Ollama.OllamaChatClient>(client =>
{
    client.BaseAddress = new Uri(ollamaEndpoint);
});

builder.Services.AddHttpClient<Microsoft.Extensions.AI.IEmbeddingGenerator<string, Microsoft.Extensions.AI.Embedding<float>>, Palantir.AiAssistantService.Ollama.OllamaEmbeddingGenerator>(client =>
{
    client.BaseAddress = new Uri(ollamaEndpoint);
});

// Multi-Agent Ecosystem
builder.Services.AddScoped<Palantir.AiAssistantService.Agents.TradeAnalysisAgent>();
builder.Services.AddScoped<Palantir.AiAssistantService.Agents.RiskGuardAgent>();
builder.Services.AddScoped<Palantir.AiAssistantService.Agents.TradeCopilotCoordinator>();

// Governance & Presidio PII Redaction
builder.Services.AddSingleton<Palantir.AiAssistantService.Governance.IPiiRedactor, Palantir.AiAssistantService.Governance.PresidioPiiRedactor>();
builder.Services.AddSingleton<Palantir.AiAssistantService.Governance.AgentGovernanceManager>();

// A2A Protocol Client
builder.Services.AddHttpClient<Palantir.AiAssistantService.Protocols.IA2AClient, Palantir.AiAssistantService.Protocols.A2AClient>();

builder.Services.AddEndpoints(typeof(Program).Assembly);

var app = builder.Build();

app.MapDefaultEndpoints();
app.UsePalantirExceptionHandler();

app.MapEndpoints();

app.Run();

public class QdrantCollectionInitializer(QdrantClient qdrantClient, ILogger<QdrantCollectionInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var exists = await qdrantClient.CollectionExistsAsync("market_sentiment", cancellationToken);
            if (!exists)
            {
                await qdrantClient.CreateCollectionAsync(
                    "market_sentiment",
                    vectorsConfig: new VectorParams { Size = 128, Distance = Distance.Cosine },
                    cancellationToken: cancellationToken);
                logger.LogInformation("Qdrant collection 'market_sentiment' created successfully with Cosine distance.");
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not initialize Qdrant collection 'market_sentiment'. It may be created later.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
