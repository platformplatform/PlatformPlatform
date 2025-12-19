using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Tests.Persistence;
using PlatformPlatform.SharedKernel.Tests.TestEntities;
using Xunit;

namespace PlatformPlatform.SharedKernel.Tests.EntityFramework;

public sealed class UseStringForEnumsTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SqliteInMemoryDbContextFactory<TestDbContext> _sqliteInMemoryDbContextFactory;
    private readonly TestDbContext _testDbContext;

    public UseStringForEnumsTests()
    {
        var executionContext = new BackgroundWorkerExecutionContext();
        _sqliteInMemoryDbContextFactory = new SqliteInMemoryDbContextFactory<TestDbContext>(executionContext, TimeProvider.System);
        _testDbContext = _sqliteInMemoryDbContextFactory.CreateContext();
        _connection = (SqliteConnection)_testDbContext.Database.GetDbConnection();
    }

    public void Dispose()
    {
        _sqliteInMemoryDbContextFactory.Dispose();
    }

    [Fact]
    public async Task UseStringForEnums_WhenSavingEntity_ShouldStoreEnumAsString()
    {
        // Arrange
        var testAggregate = TestAggregate.Create("Test");
        testAggregate.Status = TestStatus.Active;

        // Act
        _testDbContext.TestAggregates.Add(testAggregate);
        await _testDbContext.SaveChangesAsync();

        // Assert - Query the raw database to verify enum is stored as string
        var result = _connection.ExecuteScalar<string>(
            "SELECT Status FROM TestAggregates WHERE Id = @id",
            [new { id = testAggregate.Id }]
        );
        result.Should().Be("Active");
    }

    [Fact]
    public async Task UseStringForEnums_WhenSavingEntityWithNullableEnum_ShouldStoreEnumAsString()
    {
        // Arrange
        var testAggregate = TestAggregate.Create("Test");
        testAggregate.NullableStatus = TestStatus.Completed;

        // Act
        _testDbContext.TestAggregates.Add(testAggregate);
        await _testDbContext.SaveChangesAsync();

        // Assert - Query the raw database to verify nullable enum is stored as string
        var result = _connection.ExecuteScalar<string>(
            "SELECT NullableStatus FROM TestAggregates WHERE Id = @id",
            [new { id = testAggregate.Id }]
        );
        result.Should().Be("Completed");
    }

    [Fact]
    public async Task UseStringForEnums_WhenSavingEntityWithNullEnum_ShouldStoreNull()
    {
        // Arrange
        var testAggregate = TestAggregate.Create("Test");
        testAggregate.NullableStatus = null;

        // Act
        _testDbContext.TestAggregates.Add(testAggregate);
        await _testDbContext.SaveChangesAsync();

        // Assert - Query the raw database to verify null is stored
        var result = _connection.ExecuteScalar<string?>(
            "SELECT NullableStatus FROM TestAggregates WHERE Id = @id",
            [new { id = testAggregate.Id }]
        );
        result.Should().BeNull();
    }

    [Fact]
    public async Task UseStringForEnums_WhenReadingEntity_ShouldCorrectlyDeserializeEnums()
    {
        // Arrange
        var testAggregate = TestAggregate.Create("Test");
        testAggregate.Status = TestStatus.Completed;
        testAggregate.NullableStatus = TestStatus.Active;
        _testDbContext.TestAggregates.Add(testAggregate);
        await _testDbContext.SaveChangesAsync();
        _testDbContext.ChangeTracker.Clear();

        // Act
        var retrievedAggregate = await _testDbContext.TestAggregates.FindAsync(testAggregate.Id);

        // Assert
        retrievedAggregate.Should().NotBeNull();
        retrievedAggregate.Status.Should().Be(TestStatus.Completed);
        retrievedAggregate.NullableStatus.Should().Be(TestStatus.Active);
    }
}
