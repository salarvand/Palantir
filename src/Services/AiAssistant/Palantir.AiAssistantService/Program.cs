using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Palantir.AiAssistantService.Agents;
using Palantir.AiAssistantService.AiProvider;
using Palantir.AiAssistantService.Governance;
using Palantir.AiAssistantService.Mcp;
using Palantir.AiAssistantService.Persistence;
using Palantir.AiAssistantService.Protocols;
using Palantir.Constants;
using Palantir.Platform;
using Palantir.Platform.Persistence;
using Palantir.Platform.Security;
using Palantir.ServiceDefaults;
using Palantir.ServiceDefaults.ApiSpecification.AsyncApi;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddDefaultAsyncApi("Palantir AI Assistant Service", "1.0.0", typeof(Program));
builder.AddPalantirChassis(typeof(Program).Assembly);

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

builder.Services.AddScoped<ISentimentSearchRepository, PostgresSentimentRepository>();

// Downstream Microservice Clients for MCP Tools (with RFC 8693 Token Exchange)
builder.Services.AddHttpClient("oms", client => client.BaseAddress = new Uri($"http://{AspireResourceNames.OmsService}"))
    .AddTokenExchangeHandler(AspireResourceNames.OmsService);
builder.Services.AddHttpClient("market-data", client => client.BaseAddress = new Uri($"http://{AspireResourceNames.MarketDataService}"))
    .AddTokenExchangeHandler(AspireResourceNames.MarketDataService);
builder.Services.AddHttpClient("risk", client => client.BaseAddress = new Uri($"http://{AspireResourceNames.RiskService}"))
    .AddTokenExchangeHandler(AspireResourceNames.RiskService);
builder.Services.AddHttpClient("portfolio", client => client.BaseAddress = new Uri($"http://{AspireResourceNames.PortfolioService}"))
    .AddTokenExchangeHandler(AspireResourceNames.PortfolioService);

// Standardized Model Context Protocol (MCP) Server + Tool Executor
builder.Services.AddScoped<IMcpToolExecutor, McpToolExecutor>();
builder.Services.AddScoped<StandardMcpServer>();
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithTools<TradingMcpTools>();

// AI Provider Configuration (OpenAI-compatible IChatClient & IEmbeddingGenerator)
builder.Services.Configure<AiProviderOptions>(builder.Configuration.GetSection(AiProviderOptions.SectionName));
builder.Services.AddHttpClient<IChatClient, ApiKeyChatClient>((sp, client) =>
    AiProviderHttpGuard.ConfigureClient(client, sp.GetRequiredService<IOptions<AiProviderOptions>>().Value));
builder.Services.AddHttpClient<IEmbeddingGenerator<string, Embedding<float>>, ApiKeyEmbeddingGenerator>((sp, client) =>
    AiProviderHttpGuard.ConfigureClient(client, sp.GetRequiredService<IOptions<AiProviderOptions>>().Value));

// Multi-Agent Coordinator, Policy Governance & A2A Client
builder.Services.AddScoped<TradeAnalysisAgent>();
builder.Services.AddScoped<RiskGuardAgent>();
builder.Services.AddScoped<TradeCopilotCoordinator>();
builder.Services.AddSingleton<IPiiRedactor, PresidioPiiRedactor>();
builder.Services.AddSingleton<AgentGovernanceManager>();
builder.Services.AddHttpClient<IA2AClient, A2AClient>()
    .AddTokenExchangeHandler(AspireResourceNames.AiAssistantService);

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseDefaultAsyncApi();

await app.Services.EnsurePostgresSchemaCreatedAsync<SentimentDbContext>(
    "ai",
    app.Services.GetRequiredService<ILogger<Program>>(),
    preCreateSql: "CREATE EXTENSION IF NOT EXISTS vector SCHEMA public;");

app.UsePalantirChassis();
app.MapMcp("/mcp").RequireAuthorization();

app.Run();
