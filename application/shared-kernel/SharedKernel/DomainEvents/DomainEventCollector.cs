using Microsoft.EntityFrameworkCore;
using PlatformPlatform.SharedKernel.Domain;

namespace PlatformPlatform.SharedKernel.DomainEvents;

public sealed class DomainEventCollector(DbContext dbContext) : IDomainEventCollector
{
    public IAggregateRoot[] GetAggregatesWithDomainEvents()
    {
        return dbContext.ChangeTracker
            .Entries<IAggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Count != 0)
            .Select(e => e.Entity)
            .ToArray();
    }
}
