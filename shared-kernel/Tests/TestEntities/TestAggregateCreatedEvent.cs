using PlatformPlatform.SharedKernel.DomainCore.DomainEvents;

namespace PlatformPlatform.SharedKernel.Tests.TestEntities;

public record TestAggregateCreatedEvent(long TestAggregateId, string Name) : IDomainEvent;