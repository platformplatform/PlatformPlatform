using FluentAssertions;
using NSubstitute;
using PlatformPlatform.SharedKernel.ApplicationCore.Behaviors;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;
using PlatformPlatform.SharedKernel.DomainCore.DomainEvents;
using PlatformPlatform.SharedKernel.Tests.TestEntities;
using Xunit;

namespace PlatformPlatform.SharedKernel.Tests.ApplicationCore.Behaviors;

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
        var cancellationToken = new CancellationToken();
        var next = Substitute.For<RequestHandlerDelegate<Result<TestAggregate>>>();
        next.Invoke().Returns(TestAggregate.Create("Test"));

        var testAggregate = TestAggregate.Create("TestAggregate");
        var domainEvent = testAggregate.DomainEvents.Single(); // Get the domain events that were created.
        domainEventCollector.GetAggregatesWithDomainEvents().Returns(new[] { testAggregate });

        // Act
        await behavior.Handle(request, next, cancellationToken);

        // Assert
        await publisher.Received(1).Publish(domainEvent, cancellationToken);
        testAggregate.DomainEvents.Should().BeEmpty();
    }
}