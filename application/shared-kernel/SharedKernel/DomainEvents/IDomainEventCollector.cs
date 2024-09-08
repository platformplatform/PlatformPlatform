using PlatformPlatform.SharedKernel.Domain;

namespace PlatformPlatform.SharedKernel.DomainEvents;

public interface IDomainEventCollector
{
    IAggregateRoot[] GetAggregatesWithDomainEvents();
}
