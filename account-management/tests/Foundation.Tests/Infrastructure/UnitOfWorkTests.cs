using FluentAssertions;
using PlatformPlatform.Foundation.Domain;
using PlatformPlatform.Foundation.Infrastructure;
using PlatformPlatform.Foundation.Tests.TestEntities;
using Xunit;

namespace PlatformPlatform.Foundation.Tests.Infrastructure;

public sealed class UnitOfWorkTests : IDisposable
{
    private readonly SqliteInMemoryDbContextFactory<TestDbContext> _sqliteInMemoryDbContextFactory;
    private readonly TestDbContext _testDbContext;
    private readonly IUnitOfWork _unitOfWork;

    public UnitOfWorkTests()
    {
        _sqliteInMemoryDbContextFactory = new SqliteInMemoryDbContextFactory<TestDbContext>();
        _testDbContext = _sqliteInMemoryDbContextFactory.CreateContext();
        _unitOfWork = new UnitOfWork(_testDbContext);
    }

    public void Dispose()
    {
        _sqliteInMemoryDbContextFactory.Dispose();
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