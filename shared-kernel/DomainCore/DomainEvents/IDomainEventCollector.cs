using PlatformPlatform.SharedKernel.DomainCore.Entities;

namespace PlatformPlatform.SharedKernel.DomainCore.DomainEvents;

public interface IDomainEventCollector
{
    IEnumerable<IAggregateRoot> GetAggregatesWithDomainEvents();
}