using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Tests.Persistence;
using PlatformPlatform.SharedKernel.Tests.TestEntities;
using Xunit;

namespace PlatformPlatform.SharedKernel.Tests.EntityFramework;

public sealed class MapStronglyTypedStringTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SqliteInMemoryDbContextFactory<TestDbContext> _sqliteInMemoryDbContextFactory;
    private readonly TestDbContext _testDbContext;

    public MapStronglyTypedStringTests()
    {
        var executionContext = new BackgroundWorkerExecutionContext();
        _sqliteInMemoryDbContextFactory = new SqliteInMemoryDbContextFactory<TestDbContext>(executionContext);
        _testDbContext = _sqliteInMemoryDbContextFactory.CreateContext();
        _connection = (SqliteConnection)_testDbContext.Database.GetDbConnection();
    }

    public void Dispose()
    {
        _sqliteInMemoryDbContextFactory.Dispose();
    }

    [Fact]
    public async Task MapStronglyTypedString_WhenSavingEntity_ShouldStoreStringValue()
    {
        // Arrange
        var testAggregate = TestAggregate.Create("Test");
        testAggregate.ExternalId = ExternalId.NewId("ext_abc123");

        // Act
        _testDbContext.TestAggregates.Add(testAggregate);
        await _testDbContext.SaveChangesAsync();

        // Assert
        var result = _connection.ExecuteScalar<string>(
            "SELECT ExternalId FROM TestAggregates WHERE Id = @id",
            [new { id = testAggregate.Id }]
        );
        result.Should().Be("ext_abc123");
    }

    [Fact]
    public async Task MapStronglyTypedString_WhenReadingEntity_ShouldDeserializeCorrectly()
    {
        // Arrange
        const long id = 123;
        _connection.Insert("TestAggregates",
            [
                ("Id", id),
                ("Name", "Test"),
                ("Status", "Pending"),
                ("ExternalId", "ext_xyz789"),
                ("CreatedAt", DateTime.UtcNow.ToString("O"))
            ]
        );

        // Act
        var retrievedAggregate = await _testDbContext.TestAggregates.FindAsync(id);

        // Assert
        retrievedAggregate.Should().NotBeNull();
        retrievedAggregate.ExternalId.Value.Should().Be("ext_xyz789");
    }

    [Fact]
    public async Task MapStronglyTypedString_WhenQueryingByStringId_ShouldFindEntity()
    {
        // Arrange
        var externalId = ExternalId.NewId("ext_findme");
        _connection.Insert("TestAggregates",
            [
                ("Id", 456L),
                ("Name", "Test"),
                ("Status", "Pending"),
                ("ExternalId", externalId.Value),
                ("CreatedAt", DateTime.UtcNow.ToString("O"))
            ]
        );

        // Act
        var retrievedAggregate = await _testDbContext.TestAggregates
            .FirstOrDefaultAsync(e => e.ExternalId == externalId);

        // Assert
        retrievedAggregate.Should().NotBeNull();
        retrievedAggregate.Name.Should().Be("Test");
    }
}
