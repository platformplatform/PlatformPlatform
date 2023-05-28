using FluentAssertions;
using MediatR;
using NSubstitute;
using PlatformPlatform.Foundation.DomainModeling.Behaviors;
using PlatformPlatform.Foundation.DomainModeling.Cqrs;
using PlatformPlatform.Foundation.DomainModeling.DomainEvents;
using PlatformPlatform.Foundation.Tests.TestEntities;
using Xunit;

namespace PlatformPlatform.Foundation.Tests.DomainModeling.Behaviors;

public class PublishDomainEventsPipelineBehaviorTests
{
    [Fact]
    public async Task Handle_WhenCalled_ShouldPublishDomainEvents()
    {
        // Arrange
        var domainEventCollector = Substitute.For<IDomainEventCollector>();
        var publisher = Substitute.For<IPublisher>();
        var behavior =
            new PublishDomainEventsPipelineBehavior<TestCommand, Result<TestAggregate>>(domainEventCollector,
                publisher);
        var request = new TestCommand();
        var cancellationToken = new CancellationToken();
        var next = Substitute.For<RequestHandlerDelegate<Result<TestAggregate>>>();
        next.Invoke().Returns(TestAggregate.Create("Test"));

        var testAggregate = TestAggregate.Create("TestAggregate");
        var domainEvent = testAggregate.DomainEvents.Single(); // Get the domain events that were created.
        domainEventCollector.GetAggregatesWithDomainEvents().Returns(new[] {testAggregate});

        // Act
        await behavior.Handle(request, next, cancellationToken);

        // Assert
        await publisher.Received(1).Publish(domainEvent, cancellationToken);
        testAggregate.DomainEvents.Should().BeEmpty();
    }
}