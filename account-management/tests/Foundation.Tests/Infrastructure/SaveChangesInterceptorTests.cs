using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PlatformPlatform.Foundation.Tests.TestEntities;
using Xunit;

namespace PlatformPlatform.Foundation.Tests.Infrastructure;

public class SaveChangesInterceptorTests
{
    private readonly TestDbContext _testDbContext;

    public SaveChangesInterceptorTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _testDbContext = new TestDbContext(options);
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