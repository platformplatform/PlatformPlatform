using FluentAssertions;
using PlatformPlatform.Foundation.Tests.TestEntities;
using Xunit;

namespace PlatformPlatform.Foundation.Tests.Domain;

public class DomainEventTests
{
    [Fact]
    public void DomainEvent_WhenCreatingDomainEvents_ShouldTrackEventWithCorrectOccurrence()
    {
        // Act
        var testAggregate = TestAggregate.Create("test");

        // Assert
        testAggregate.DomainEvents.Count.Should().Be(1);
        var domainEvent = (TestAggregateCreatedEvent) testAggregate.DomainEvents.Single();
        domainEvent.TestAggregateId.Should().Be(testAggregate.Id);
        domainEvent.Name.Should().Be("test");
    }
}