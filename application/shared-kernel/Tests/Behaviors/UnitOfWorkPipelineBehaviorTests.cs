using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SharedKernel.Cqrs;
using SharedKernel.Persistence;
using SharedKernel.PipelineBehaviors;
using SharedKernel.Tests.TestEntities;
using Xunit;

namespace SharedKernel.Tests.Behaviors;

public sealed class UnitOfWorkPipelineBehaviorTests
{
    private readonly UnitOfWorkPipelineBehavior<TestCommand, Result<TestAggregate>> _behavior;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUnitOfWork _unitOfWork;

    public UnitOfWorkPipelineBehaviorTests()
    {
        var services = new ServiceCollection();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        services.AddSingleton(_unitOfWork);
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        _behavior = new UnitOfWorkPipelineBehavior<TestCommand, Result<TestAggregate>>(
            _unitOfWork,
            new ConcurrentCommandCounter(),
            _httpContextAccessor
        );
    }

    [Fact]
    public async Task Handle_WhenSuccessfulCommand_ShouldCallNextAndCommitChanges()
    {
        // Arrange
        var command = new TestCommand();
        var next = Substitute.For<RequestHandlerDelegate<Result<TestAggregate>>>();
        var successfulCommandResult = Result<TestAggregate>.Success(TestAggregate.Create("Foo"));
        next.Invoke().Returns(Task.FromResult(successfulCommandResult));

        // Act
        _ = await _behavior.Handle(command, next, CancellationToken.None);

        // Assert
        await _unitOfWork.Received().CommitAsync(CancellationToken.None);
        Received.InOrder(() =>
            {
                next.Invoke();
                _unitOfWork.CommitAsync(CancellationToken.None);
            }
        );
    }

    [Fact]
    public async Task Handle_WhenNonSuccessfulCommand_ShouldCallNextButNotCommitChanges()
    {
        // Arrange
        var command = new TestCommand();
        var next = Substitute.For<RequestHandlerDelegate<Result<TestAggregate>>>();
        var successfulCommandResult = Result<TestAggregate>.BadRequest("Fail");
        next.Invoke().Returns(Task.FromResult(successfulCommandResult));

        // Act
        _ = await _behavior.Handle(command, next, CancellationToken.None);

        // Assert
        await _unitOfWork.DidNotReceive().CommitAsync(CancellationToken.None);
        await next.Received().Invoke();
    }

    [Fact]
    public async Task Handle_WhenCommitReturnsTxid_ShouldSetElectricOffsetHeader()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        _httpContextAccessor.HttpContext.Returns(httpContext);
        _unitOfWork.CommitAsync(CancellationToken.None).Returns("12345");

        var command = new TestCommand();
        var next = Substitute.For<RequestHandlerDelegate<Result<TestAggregate>>>();
        var successfulCommandResult = Result<TestAggregate>.Success(TestAggregate.Create("Foo"));
        next.Invoke().Returns(Task.FromResult(successfulCommandResult));

        // Act
        _ = await _behavior.Handle(command, next, CancellationToken.None);

        // Assert
        httpContext.Response.Headers["electric-offset"].ToString().Should().Be("12345");
    }

    [Fact]
    public async Task Handle_WhenCommitReturnsNull_ShouldNotSetElectricOffsetHeader()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        _httpContextAccessor.HttpContext.Returns(httpContext);
        _unitOfWork.CommitAsync(CancellationToken.None).Returns((string?)null);

        var command = new TestCommand();
        var next = Substitute.For<RequestHandlerDelegate<Result<TestAggregate>>>();
        var successfulCommandResult = Result<TestAggregate>.Success(TestAggregate.Create("Foo"));
        next.Invoke().Returns(Task.FromResult(successfulCommandResult));

        // Act
        _ = await _behavior.Handle(command, next, CancellationToken.None);

        // Assert
        httpContext.Response.Headers.ContainsKey("electric-offset").Should().BeFalse();
    }
}
