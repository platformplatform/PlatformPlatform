using PlatformPlatform.Foundation.Domain;

namespace PlatformPlatform.Foundation.Tests.TestEntities;

public record TestAggregateCreatedEvent(long TestAggregateId, string Name) : IDomainEvent;