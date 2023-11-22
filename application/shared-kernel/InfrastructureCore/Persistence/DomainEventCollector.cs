using Microsoft.EntityFrameworkCore;
using PlatformPlatform.SharedKernel.DomainCore.DomainEvents;
using PlatformPlatform.SharedKernel.DomainCore.Entities;

namespace PlatformPlatform.SharedKernel.InfrastructureCore.Persistence;

public sealed class DomainEventCollector(DbContext dbContext) : IDomainEventCollector
{
    public IEnumerable<IAggregateRoot> GetAggregatesWithDomainEvents()
    {
        return dbContext.ChangeTracker
            .Entries<IAggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Count != 0)
            .Select(e => e.Entity);
    }
}