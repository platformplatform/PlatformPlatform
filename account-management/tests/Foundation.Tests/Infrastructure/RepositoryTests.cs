using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PlatformPlatform.Foundation.Domain;
using PlatformPlatform.Foundation.Tests.TestEntities;
using Xunit;

namespace PlatformPlatform.Foundation.Tests.Infrastructure;

public sealed class RepositoryTests : IDisposable
{
    private readonly TestAggregateRepository _testAggregateRepository;
    private readonly TestDbContext _testDbContext;
    private readonly DbContextOptions<TestDbContext> _testDbContextOptions;

    public RepositoryTests()
    {
        _testDbContextOptions = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _testDbContext = new TestDbContext(_testDbContextOptions);
        _testAggregateRepository = new TestAggregateRepository(_testDbContext);
    }

    public void Dispose()
    {
        _testDbContext.Database.EnsureDeleted();
    }

    [Fact]
    public async Task Add_WhenNewAggregate_ShouldAddToDatabase()
    {
        // Arrange
        var testAggregate = TestAggregate.Create("TestAggregate");

        // Act
        _testAggregateRepository.Add(testAggregate);
        await _testDbContext.SaveChangesAsync();

        // Assert
        var retrievedAggregate = await _testAggregateRepository.GetByIdAsync(testAggregate.Id, CancellationToken.None);
        retrievedAggregate.Should().NotBeNull();
        retrievedAggregate!.Id.Should().Be(testAggregate.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenAggregateExists_ShouldRetrieveFromDatabase()
    {
        // Arrange
        var testAggregate = TestAggregate.Create("TestAggregate");
        _testDbContext.TestAggregates.Add(testAggregate);
        await _testDbContext.SaveChangesAsync();

        // Act
        var retrievedAggregate = await _testAggregateRepository.GetByIdAsync(testAggregate.Id, CancellationToken.None);

        // Assert
        retrievedAggregate.Should().NotBeNull();
        retrievedAggregate!.Id.Should().Be(testAggregate.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenAggregateDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = IdGenerator.NewId();

        // Act
        var retrievedAggregate = await _testAggregateRepository.GetByIdAsync(nonExistentId, CancellationToken.None);

        // Assert
        retrievedAggregate.Should().BeNull();
    }

    [Fact]
    public async Task Update_WhenExistingAggregate_ShouldUpdateDatabase()
    {
        // Arrange
        var testAggregate = TestAggregate.Create("TestAggregate");
        _testDbContext.TestAggregates.Add(testAggregate);
        await _testDbContext.SaveChangesAsync();
        var initialName = testAggregate.Name;

        // Act
        testAggregate.Name = "UpdatedName";
        _testAggregateRepository.Update(testAggregate);
        await _testDbContext.SaveChangesAsync();

        // Assert
        var updatedAggregate = await _testAggregateRepository.GetByIdAsync(testAggregate.Id, CancellationToken.None);
        updatedAggregate.Should().NotBeNull();
        updatedAggregate!.Name.Should().NotBe(initialName);
        updatedAggregate.Name.Should().Be("UpdatedName");
    }

    [Fact]
    public async Task Remove_WhenExistingAggregate_ShouldRemoveFromDatabase()
    {
        // Arrange
        var testAggregate = TestAggregate.Create("TestAggregate");
        _testDbContext.TestAggregates.Add(testAggregate);
        await _testDbContext.SaveChangesAsync();

        // Act
        _testAggregateRepository.Remove(testAggregate);
        await _testDbContext.SaveChangesAsync();

        // Assert
        var retrievedAggregate = await _testAggregateRepository.GetByIdAsync(testAggregate.Id, CancellationToken.None);
        retrievedAggregate.Should().BeNull();
    }
}