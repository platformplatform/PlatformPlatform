using Microsoft.EntityFrameworkCore;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.DomainEvents;

namespace PlatformPlatform.SharedKernel.Persistence;

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
