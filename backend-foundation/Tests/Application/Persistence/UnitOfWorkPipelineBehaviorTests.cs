using MediatR;
using NSubstitute;
using PlatformPlatform.Foundation.DomainModeling.Behaviors;
using PlatformPlatform.Foundation.DomainModeling.Persistence;
using PlatformPlatform.Foundation.Tests.TestEntities;
using Xunit;

namespace PlatformPlatform.Foundation.Tests.Application.Persistence;

public class UnitOfWorkPipelineBehaviorTests
{
    [Fact]
    public async Task Handle_WhenCalled_ShouldCallNextAndCommitChanges()
    {
        // Arrange
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var behavior = new UnitOfWorkPipelineBehavior<TestCommand, Task>(unitOfWork);
        var request = new TestCommand();
        var cancellationToken = new CancellationToken();
        var next = Substitute.For<RequestHandlerDelegate<Task>>();
        next.Invoke().Returns(Task.CompletedTask);

        // Act
        await behavior.Handle(request, next, cancellationToken);

        // Assert
        await unitOfWork.Received().CommitAsync(cancellationToken);
        Received.InOrder(() =>
        {
            next.Invoke();
            unitOfWork.CommitAsync(cancellationToken);
        });
    }
}