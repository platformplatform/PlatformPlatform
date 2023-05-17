using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PlatformPlatform.Foundation.Domain;
using PlatformPlatform.Foundation.Infrastructure;
using PlatformPlatform.Foundation.Tests.TestEntities;
using Xunit;

namespace PlatformPlatform.Foundation.Tests.Infrastructure;

public class UnitOfWorkTests
{
    private readonly TestDbContext _testDbContext;
    private readonly IUnitOfWork _unitOfWork;

    public UnitOfWorkTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _testDbContext = new TestDbContext(options);
        _unitOfWork = new UnitOfWork(_testDbContext);
    }

    [Fact]
    public async Task CommitAsync_WhenAggregateIsAdded_ShouldSetCreatedAt()
    {
        // Arrange
        var newTestAggregate = TestAggregate.Create("TestAggregate");

        // Act
        _testDbContext.TestAggregates.Add(newTestAggregate);
        newTestAggregate.ClearDomainEvents(); // Simulate that domain events have been handled
        await _unitOfWork.CommitAsync(CancellationToken.None);

        // Assert
        newTestAggregate.CreatedAt.Should().NotBe(default);
        newTestAggregate.ModifiedAt.Should().BeNull();
    }

    [Fact]
    public async Task CommitAsync_WhenAggregateIsModified_ShouldUpdateModifiedAt()
    {
        // Arrange
        var newTestAggregate = TestAggregate.Create("TestAggregate");
        _testDbContext.TestAggregates.Add(newTestAggregate);
        newTestAggregate.ClearDomainEvents(); // Simulate that domain events have been handled
        await _unitOfWork.CommitAsync(CancellationToken.None);
        var initialCreatedAt = newTestAggregate.CreatedAt;

        // Act
        newTestAggregate.Name = "UpdatedTestAggregate";
        await _unitOfWork.CommitAsync(CancellationToken.None);

        // Assert
        newTestAggregate.ModifiedAt.Should().NotBeNull();
        newTestAggregate.ModifiedAt.Should().BeAfter(initialCreatedAt);
        newTestAggregate.CreatedAt.Should().Be(initialCreatedAt);
    }

    [Fact]
    public async Task CommitAsync_WhenAggregateHasUnhandledDomainEvents_ShouldThrowException()
    {
        // Arrange
        var newTestAggregate = TestAggregate.Create("TestAggregate");
        _testDbContext.TestAggregates.Add(newTestAggregate);

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(() => _unitOfWork.CommitAsync(CancellationToken.None));
    }
}