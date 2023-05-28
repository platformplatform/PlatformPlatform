using FluentAssertions;
using PlatformPlatform.SharedKernel.Tests.TestEntities;
using Xunit;

namespace PlatformPlatform.SharedKernel.Tests.DomainCore.DomainEvents;

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