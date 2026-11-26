using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Palantir.AiAssistantService.AiProvider;
using Palantir.AiAssistantService.Mcp;
using Palantir.AiAssistantService.Persistence;
using Palantir.Constants;
using Palantir.Platform.Caching;
using Palantir.Platform.Endpoints;
using Palantir.Platform.Exceptions;
using Palantir.Platform.Persistence;
using Palantir.Platform.Security;
using Palantir.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var connectionString = builder.Configuration.GetConnectionString(AspireResourceNames.PalantirDb)
                       ?? "Host=localhost;Database=PalantirDb;Username=postgres;Password=postgres";

builder.Services.AddDbContext<SentimentDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsql =>
    {
        npgsql.UseVector();
        npgsql.EnableRetryOnFailure(10, TimeSpan.FromSeconds(5), null);
    });
});

builder.Services.AddPalantirCaching(connectionString);
builder.Services.AddPalantirSecurity();
builder.Services.AddPalantirProblemDetails();
builder.Services.AddScoped<ISentimentSearchRepository, PostgresSentimentRepository>();

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
builder.Services.AddScoped<StandardMcpServer>();

// AI Provider Configuration (API Key loaded from User Secrets / Environment)
builder.Services.Configure<AiProviderOptions>(builder.Configuration.GetSection(AiProviderOptions.SectionName));

builder.Services.AddHttpClient<IChatClient, ApiKeyChatClient>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<AiProviderOptions>>().Value;
    AiProviderHttpGuard.ConfigureClient(client, options);
});

builder.Services.AddHttpClient<IEmbeddingGenerator<string, Embedding<float>>, ApiKeyEmbeddingGenerator>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<AiProviderOptions>>().Value;
    AiProviderHttpGuard.ConfigureClient(client, options);
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

await app.Services.EnsurePostgresSchemaCreatedAsync<SentimentDbContext>(
    "ai",
    app.Services.GetRequiredService<ILogger<Program>>(),
    preCreateSql: "CREATE EXTENSION IF NOT EXISTS vector SCHEMA public;");

app.MapEndpoints();

app.Run();
