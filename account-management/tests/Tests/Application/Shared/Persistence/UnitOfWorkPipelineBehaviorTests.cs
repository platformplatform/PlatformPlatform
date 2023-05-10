using MediatR;
using NSubstitute;
using PlatformPlatform.AccountManagement.Application.Shared.Persistence;
using PlatformPlatform.AccountManagement.Domain.Shared;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Application.Shared.Persistence;

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

public record TestCommand : IRequest;