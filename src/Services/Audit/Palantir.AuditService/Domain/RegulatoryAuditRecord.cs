using System.Text.Json.Serialization;

namespace Palantir.AuditService.Domain;

public record RegulatoryAuditRecord(
    [property: JsonPropertyName("auditId")] Guid AuditId,
    [property: JsonPropertyName("eventType")] string EventType, // OrderPlaced, PreTradeRiskApproved, TradeMatched, SettlementCompleted
    [property: JsonPropertyName("orderId")] Guid OrderId,
    [property: JsonPropertyName("tradeId")] Guid? TradeId,
    [property: JsonPropertyName("clientId")] Guid ClientId,
    [property: JsonPropertyName("symbol")] string Symbol,
    [property: JsonPropertyName("price")] decimal Price,
    [property: JsonPropertyName("quantity")] decimal Quantity,
    [property: JsonPropertyName("payloadJson")] string PayloadJson,
    [property: JsonPropertyName("timestampUtc")] DateTimeOffset TimestampUtc,
    [property: JsonPropertyName("traceId")] string? TraceId = null
);
