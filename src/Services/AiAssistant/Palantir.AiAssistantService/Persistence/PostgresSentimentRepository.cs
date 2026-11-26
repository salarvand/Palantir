using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace Palantir.AiAssistantService.Persistence;

public sealed record SentimentMatch(
    string Symbol,
    string Headline,
    string SentimentLabel,
    float SentimentScore,
    double SimilarityScore,
    DateTimeOffset PublishedAtUtc);

public interface ISentimentSearchRepository
{
    Task<IReadOnlyList<SentimentMatch>> SearchAsync(
        string symbol,
        float[] queryEmbedding,
        int limit = 5,
        float minSimilarity = 0.70f,
        CancellationToken ct = default);
}

public sealed class PostgresSentimentRepository : ISentimentSearchRepository
{
    private readonly SentimentDbContext _db;

    public PostgresSentimentRepository(SentimentDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SentimentMatch>> SearchAsync(
        string symbol,
        float[] queryEmbedding,
        int limit = 5,
        float minSimilarity = 0.70f,
        CancellationToken ct = default)
    {
        var queryVector = new Vector(queryEmbedding);
        var upperSymbol = symbol.Trim().ToUpperInvariant();
        var maxDistance = 1.0 - minSimilarity;

        return await _db.Sentiments
            .AsNoTracking()
            .Where(x => x.Symbol == upperSymbol && x.Embedding.CosineDistance(queryVector) <= maxDistance)
            .OrderBy(x => x.Embedding.CosineDistance(queryVector))
            .Take(Math.Clamp(limit, 1, 20))
            .Select(x => new SentimentMatch(
                x.Symbol,
                x.Headline,
                x.SentimentLabel,
                x.SentimentScore,
                1.0 - x.Embedding.CosineDistance(queryVector),
                x.PublishedAtUtc))
            .ToListAsync(ct);
    }
}
