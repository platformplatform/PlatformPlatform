using PlatformPlatform.Foundation.DomainModeling.DomainEvents;

namespace PlatformPlatform.Foundation.Tests.TestEntities;

public record TestAggregateCreatedEvent(long TestAggregateId, string Name) : IDomainEvent;