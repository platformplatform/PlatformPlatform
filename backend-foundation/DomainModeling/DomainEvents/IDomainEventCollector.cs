using PlatformPlatform.Foundation.DomainModeling.Entities;

namespace PlatformPlatform.Foundation.DomainModeling.DomainEvents;

public interface IDomainEventCollector
{
    IEnumerable<IAggregateRoot> GetAggregatesWithDomainEvents();
}