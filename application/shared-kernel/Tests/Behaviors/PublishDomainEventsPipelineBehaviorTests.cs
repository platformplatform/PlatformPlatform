using FluentAssertions;
using NSubstitute;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.DomainEvents;
using PlatformPlatform.SharedKernel.PipelineBehaviors;
using PlatformPlatform.SharedKernel.Tests.TestEntities;
using Xunit;

namespace PlatformPlatform.SharedKernel.Tests.Behaviors;

public sealed class PublishDomainEventsPipelineBehaviorTests
{
    [Fact]
    public async Task Handle_WhenCalled_ShouldPublishDomainEvents()
    {
        // Arrange
        var domainEventCollector = Substitute.For<IDomainEventCollector>();
        var publisher = Substitute.For<IPublisher>();
        var behavior = new PublishDomainEventsPipelineBehavior<TestCommand, Result<TestAggregate>>(
            domainEventCollector,
            publisher
        );
        var request = new TestCommand();
        var next = Substitute.For<RequestHandlerDelegate<Result<TestAggregate>>>();
        next.Invoke().Returns(TestAggregate.Create("Test"));

        var testAggregate = TestAggregate.Create("TestAggregate");
        var domainEvent = testAggregate.DomainEvents.Single(); // Get the domain events that were created
        domainEventCollector.GetAggregatesWithDomainEvents().Returns(
            _ => testAggregate.DomainEvents.Count == 0 ? [] : [testAggregate]
        );

        // Act
        await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        await publisher.Received(1).Publish(domainEvent, CancellationToken.None);
        testAggregate.DomainEvents.Should().BeEmpty();
    }
}
