using FluentAssertions;
using PlatformPlatform.Foundation.Tests.TestEntities;
using Xunit;

namespace PlatformPlatform.Foundation.Tests.InfrastructureCore.EntityFramework;

public sealed class SaveChangesInterceptorTests : IDisposable
{
    private readonly SqliteInMemoryDbContextFactory<TestDbContext> _sqliteInMemoryDbContextFactory;
    private readonly TestDbContext _testDbContext;

    public SaveChangesInterceptorTests()
    {
        _sqliteInMemoryDbContextFactory = new SqliteInMemoryDbContextFactory<TestDbContext>();
        _testDbContext = _sqliteInMemoryDbContextFactory.CreateContext();
    }

    public void Dispose()
    {
        _sqliteInMemoryDbContextFactory.Dispose();
    }

    [Fact]
    public async Task SavingChangesAsync_WhenEntityIsAdded_ShouldSetCreatedAt()
    {
        // Arrange
        var newTestAggregate = TestAggregate.Create("TestAggregate");

        // Act
        _testDbContext.TestAggregates.Add(newTestAggregate);
        await _testDbContext.SaveChangesAsync();

        // Assert
        newTestAggregate.CreatedAt.Should().NotBe(default);
        newTestAggregate.ModifiedAt.Should().BeNull();
    }

    [Fact]
    public async Task SavingChangesAsync_WhenEntityIsModified_ShouldUpdateModifiedAt()
    {
        // Arrange
        var newTestAggregate = TestAggregate.Create("TestAggregate");
        _testDbContext.TestAggregates.Add(newTestAggregate);
        await _testDbContext.SaveChangesAsync();
        var initialCreatedAt = newTestAggregate.CreatedAt;
        var initialModifiedAt = newTestAggregate.ModifiedAt;

        // Act
        newTestAggregate.Name = "UpdatedTestAggregate";
        await _testDbContext.SaveChangesAsync();

        // Assert
        newTestAggregate.ModifiedAt.Should().NotBe(default);
        newTestAggregate.ModifiedAt.Should().NotBe(initialModifiedAt);
        newTestAggregate.CreatedAt.Should().Be(initialCreatedAt);
    }
}