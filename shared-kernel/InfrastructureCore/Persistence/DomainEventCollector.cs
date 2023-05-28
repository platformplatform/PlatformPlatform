using Microsoft.EntityFrameworkCore;
using PlatformPlatform.Foundation.DomainModeling.DomainEvents;
using PlatformPlatform.Foundation.DomainModeling.Entities;

namespace PlatformPlatform.Foundation.InfrastructureCore.Persistence;

public sealed class DomainEventCollector : IDomainEventCollector
{
    private readonly DbContext _dbContext;

    public DomainEventCollector(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IEnumerable<IAggregateRoot> GetAggregatesWithDomainEvents()
    {
        return _dbContext.ChangeTracker
            .Entries<IAggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity);
    }
}