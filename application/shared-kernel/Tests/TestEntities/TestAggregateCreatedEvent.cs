using SharedKernel.DomainEvents;

namespace SharedKernel.Tests.TestEntities;

public record TestAggregateCreatedEvent(long TestAggregateId, string Name) : IDomainEvent;
