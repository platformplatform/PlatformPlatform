using PlatformPlatform.SharedKernel.DomainModeling.DomainEvents;

namespace PlatformPlatform.SharedKernel.Tests.TestEntities;

public record TestAggregateCreatedEvent(long TestAggregateId, string Name) : IDomainEvent;