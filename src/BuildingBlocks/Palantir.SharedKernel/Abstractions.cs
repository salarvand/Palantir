namespace Palantir.SharedKernel;

public interface IEntity<out TId>
{
    TId Id { get; }
}

public interface IAggregateRoot<out TId> : IEntity<TId>
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}

public interface IDomainEvent
{
    Guid EventId { get; }
    DateTimeOffset TimestampUtc { get; }
    string CorrelationId { get; }
}

public interface IAuditableEntity
{
    DateTimeOffset CreatedAtUtc { get; set; }
    string? CreatedBy { get; set; }
    DateTimeOffset? LastModifiedAtUtc { get; set; }
    string? LastModifiedBy { get; set; }
}

public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTimeOffset? DeletedAtUtc { get; set; }
    string? DeletedBy { get; set; }
}
