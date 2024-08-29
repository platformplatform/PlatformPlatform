using PlatformPlatform.SharedKernel.Entities;

namespace PlatformPlatform.SharedKernel.DomainEvents;

public interface IDomainEventCollector
{
    IAggregateRoot[] GetAggregatesWithDomainEvents();
}
