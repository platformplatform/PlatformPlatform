using SharedKernel.Domain;

namespace SharedKernel.DomainEvents;

public interface IDomainEventCollector
{
    IAggregateRoot[] GetAggregatesWithDomainEvents();
}
