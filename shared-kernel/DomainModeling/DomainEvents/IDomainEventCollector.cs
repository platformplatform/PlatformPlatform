using PlatformPlatform.SharedKernel.DomainModeling.Entities;

namespace PlatformPlatform.SharedKernel.DomainModeling.DomainEvents;

public interface IDomainEventCollector
{
    IEnumerable<IAggregateRoot> GetAggregatesWithDomainEvents();
}