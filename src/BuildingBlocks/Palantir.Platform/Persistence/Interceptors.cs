using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Palantir.SharedKernel;
using Wolverine;

namespace Palantir.Platform.Persistence;

public class PublishDomainEventsInterceptor : SaveChangesInterceptor
{
    private readonly IMessageBus? _messageBus;

    public PublishDomainEventsInterceptor(IMessageBus? messageBus = null)
    {
        _messageBus = messageBus;
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null || _messageBus is null)
        {
            return await base.SavedChangesAsync(eventData, result, cancellationToken);
        }

        var aggregateRoots = eventData.Context.ChangeTracker
            .Entries()
            .Where(e => e.Entity.GetType().GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAggregateRoot<>)))
            .Select(e => e.Entity)
            .ToList();

        foreach (var aggregate in aggregateRoots)
        {
            var clearMethod = aggregate.GetType().GetMethod("ClearDomainEvents");
            var eventsProp = aggregate.GetType().GetProperty("DomainEvents");

            if (eventsProp?.GetValue(aggregate) is IReadOnlyCollection<IDomainEvent> domainEvents)
            {
                foreach (var domainEvent in domainEvents)
                {
                    await _messageBus.PublishAsync(domainEvent);
                }
            }

            clearMethod?.Invoke(aggregate, null);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }
}

public class AuditableEntityInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public AuditableEntityInterceptor(IHttpContextAccessor? httpContextAccessor = null)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateAuditEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateAuditEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateAuditEntities(DbContext? context)
    {
        if (context is null) return;

        var now = DateTimeOffset.UtcNow;
        var currentUserId = _httpContextAccessor?.HttpContext?.User?.FindFirst("sub")?.Value
                            ?? _httpContextAccessor?.HttpContext?.User?.Identity?.Name
                            ?? "system";

        foreach (var entry in context.ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = now;
                entry.Entity.CreatedBy = currentUserId;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.LastModifiedAtUtc = now;
                entry.Entity.LastModifiedBy = currentUserId;
            }
        }

        foreach (var entry in context.ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedAtUtc = now;
                entry.Entity.DeletedBy = currentUserId;
            }
        }
    }
}
