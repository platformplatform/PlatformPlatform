using FluentAssertions;
using JetBrains.Annotations;
using PlatformPlatform.Foundation.Domain;
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

public record TestAggregateCreatedEvent(long TestAggregateId, string Name) : IDomainEvent;

public sealed class TestAggregate : AggregateRoot<long>
{
    private TestAggregate(string name) : base(IdGenerator.NewId())
    {
        Name = name;
    }

    [UsedImplicitly]
    public string Name { get; set; }

    public static TestAggregate Create(string name)
    {
        var testAggregate = new TestAggregate(name);
        var testAggregateCreatedEvent = new TestAggregateCreatedEvent(testAggregate.Id, testAggregate.Name);
        testAggregate.AddDomainEvent(testAggregateCreatedEvent);
        return testAggregate;
    }
}