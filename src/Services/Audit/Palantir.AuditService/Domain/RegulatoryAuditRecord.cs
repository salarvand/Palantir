namespace Palantir.AuditService.Domain;

public record RegulatoryAuditRecord(
    Guid AuditId,
    string EventType, // OrderPlaced, PreTradeRiskApproved, TradeMatched, SettlementCompleted
    Guid OrderId,
    Guid? TradeId,
    Guid ClientId,
    string Symbol,
    decimal Price,
    decimal Quantity,
    string PayloadJson,
    DateTimeOffset TimestampUtc
);
