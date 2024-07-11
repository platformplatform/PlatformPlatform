using PlatformPlatform.SharedKernel.Domain.Entities;

namespace PlatformPlatform.SharedKernel.Domain.DomainEvents;

public interface IDomainEventCollector
{
    IAggregateRoot[] GetAggregatesWithDomainEvents();
}
