using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Tests.TestEntities;
using Xunit;

namespace PlatformPlatform.SharedKernel.Tests.EntityFramework;

public sealed class SoftDeleteTests : IDisposable
{
    private readonly SqliteInMemoryDbContextFactory<SoftDeletableTestDbContext> _sqliteInMemoryDbContextFactory;

    public SoftDeleteTests()
    {
        var executionContext = new BackgroundWorkerExecutionContext();
        _sqliteInMemoryDbContextFactory = new SqliteInMemoryDbContextFactory<SoftDeletableTestDbContext>(executionContext, TimeProvider.System);
    }

    public void Dispose()
    {
        _sqliteInMemoryDbContextFactory.Dispose();
    }

    [Fact]
    public async Task Remove_WhenCalledOnSoftDeletableEntity_ShouldSetDeletedAtInsteadOfDeleting()
    {
        // Arrange
        var testDbContext1 = _sqliteInMemoryDbContextFactory.CreateContext();
        var repository1 = new SoftDeletableTestAggregateRepository(testDbContext1);
        var aggregate = SoftDeletableTestAggregate.Create("TestAggregate");
        await repository1.AddAsync(aggregate, CancellationToken.None);
        await testDbContext1.SaveChangesAsync();
        var aggregateId = aggregate.Id;
        repository1.Remove(aggregate);
        await testDbContext1.SaveChangesAsync();
        var testDbContext2 = _sqliteInMemoryDbContextFactory.CreateContext();
        var repository2 = new SoftDeletableTestAggregateRepository(testDbContext2);

        // Act
        var retrievedAggregate = await testDbContext2.SoftDeletableTestAggregates.SingleOrDefaultAsync(e => e.Id == aggregateId);
        var deletedAggregate = (await repository2.GetDeletedByIdAsync(aggregateId, CancellationToken.None))!;

        // Assert
        retrievedAggregate.Should().BeNull();
        deletedAggregate.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAllDeletedAsync_ShouldReturnOnlySoftDeletedEntities()
    {
        // Arrange
        var testDbContext = _sqliteInMemoryDbContextFactory.CreateContext();
        var repository = new SoftDeletableTestAggregateRepository(testDbContext);
        var activeAggregate = SoftDeletableTestAggregate.Create("ActiveAggregate");
        var deletedAggregate1 = SoftDeletableTestAggregate.Create("DeletedAggregate1");
        var deletedAggregate2 = SoftDeletableTestAggregate.Create("DeletedAggregate2");
        await repository.AddAsync(activeAggregate, CancellationToken.None);
        await repository.AddAsync(deletedAggregate1, CancellationToken.None);
        await repository.AddAsync(deletedAggregate2, CancellationToken.None);
        await testDbContext.SaveChangesAsync();
        repository.Remove(deletedAggregate1);
        repository.Remove(deletedAggregate2);
        await testDbContext.SaveChangesAsync();
        var testDbContext2 = _sqliteInMemoryDbContextFactory.CreateContext();
        var repository2 = new SoftDeletableTestAggregateRepository(testDbContext2);

        // Act
        var allDeleted = await repository2.GetAllDeletedAsync(CancellationToken.None);

        // Assert
        allDeleted.Should().HaveCount(2);
        allDeleted.Should().Contain(a => a.Name == "DeletedAggregate1");
        allDeleted.Should().Contain(a => a.Name == "DeletedAggregate2");
        allDeleted.Should().NotContain(a => a.Name == "ActiveAggregate");
    }

    [Fact]
    public async Task Restore_WhenCalledOnSoftDeletedEntity_ShouldClearDeletedAt()
    {
        // Arrange
        var testDbContext1 = _sqliteInMemoryDbContextFactory.CreateContext();
        var repository1 = new SoftDeletableTestAggregateRepository(testDbContext1);
        var aggregate = SoftDeletableTestAggregate.Create("TestAggregate");
        await repository1.AddAsync(aggregate, CancellationToken.None);
        await testDbContext1.SaveChangesAsync();
        var aggregateId = aggregate.Id;
        repository1.Remove(aggregate);
        await testDbContext1.SaveChangesAsync();
        var testDbContext2 = _sqliteInMemoryDbContextFactory.CreateContext();
        var repository2 = new SoftDeletableTestAggregateRepository(testDbContext2);
        var deletedAggregate = (await repository2.GetDeletedByIdAsync(aggregateId, CancellationToken.None))!;

        // Act
        repository2.Restore(deletedAggregate);
        await testDbContext2.SaveChangesAsync();

        // Assert
        var testDbContext3 = _sqliteInMemoryDbContextFactory.CreateContext();
        var restoredAggregate = (await testDbContext3.SoftDeletableTestAggregates.SingleOrDefaultAsync(e => e.Id == aggregateId))!;
        restoredAggregate.DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task PermanentlyRemove_WhenCalled_ShouldDeleteEntityFromDatabase()
    {
        // Arrange
        var testDbContext1 = _sqliteInMemoryDbContextFactory.CreateContext();
        var repository1 = new SoftDeletableTestAggregateRepository(testDbContext1);
        var aggregate = SoftDeletableTestAggregate.Create("TestAggregate");
        await repository1.AddAsync(aggregate, CancellationToken.None);
        await testDbContext1.SaveChangesAsync();
        var aggregateId = aggregate.Id;
        repository1.Remove(aggregate);
        await testDbContext1.SaveChangesAsync();
        var testDbContext2 = _sqliteInMemoryDbContextFactory.CreateContext();
        var repository2 = new SoftDeletableTestAggregateRepository(testDbContext2);
        var deletedAggregate = (await repository2.GetDeletedByIdAsync(aggregateId, CancellationToken.None))!;

        // Act
        repository2.PermanentlyRemove(deletedAggregate);
        await testDbContext2.SaveChangesAsync();

        // Assert
        var testDbContext3 = _sqliteInMemoryDbContextFactory.CreateContext();
        var repository3 = new SoftDeletableTestAggregateRepository(testDbContext3);
        var normalQuery = await testDbContext3.SoftDeletableTestAggregates.SingleOrDefaultAsync(e => e.Id == aggregateId);
        normalQuery.Should().BeNull();
        var deletedQuery = await repository3.GetDeletedByIdAsync(aggregateId, CancellationToken.None);
        deletedQuery.Should().BeNull();
    }

    [Fact]
    public async Task RestoreRange_WhenCalledOnMultipleEntities_ShouldRestoreAll()
    {
        // Arrange
        var testDbContext1 = _sqliteInMemoryDbContextFactory.CreateContext();
        var repository1 = new SoftDeletableTestAggregateRepository(testDbContext1);
        var aggregate1 = SoftDeletableTestAggregate.Create("TestAggregate1");
        var aggregate2 = SoftDeletableTestAggregate.Create("TestAggregate2");
        await repository1.AddAsync(aggregate1, CancellationToken.None);
        await repository1.AddAsync(aggregate2, CancellationToken.None);
        await testDbContext1.SaveChangesAsync();
        var id1 = aggregate1.Id;
        var id2 = aggregate2.Id;
        repository1.Remove(aggregate1);
        repository1.Remove(aggregate2);
        await testDbContext1.SaveChangesAsync();
        var testDbContext2 = _sqliteInMemoryDbContextFactory.CreateContext();
        var repository2 = new SoftDeletableTestAggregateRepository(testDbContext2);
        var deletedAggregates = await repository2.GetAllDeletedAsync(CancellationToken.None);

        // Act
        repository2.RestoreRange(deletedAggregates);
        await testDbContext2.SaveChangesAsync();

        // Assert
        var testDbContext3 = _sqliteInMemoryDbContextFactory.CreateContext();
        var repository3 = new SoftDeletableTestAggregateRepository(testDbContext3);
        var stillDeleted = await repository3.GetAllDeletedAsync(CancellationToken.None);
        stillDeleted.Should().BeEmpty();
        var restored1 = await testDbContext3.SoftDeletableTestAggregates.SingleOrDefaultAsync(e => e.Id == id1);
        var restored2 = await testDbContext3.SoftDeletableTestAggregates.SingleOrDefaultAsync(e => e.Id == id2);
        restored1.Should().NotBeNull();
        restored2.Should().NotBeNull();
    }

    [Fact]
    public async Task PermanentlyRemoveRange_WhenCalledOnMultipleEntities_ShouldDeleteAll()
    {
        // Arrange
        var testDbContext1 = _sqliteInMemoryDbContextFactory.CreateContext();
        var repository1 = new SoftDeletableTestAggregateRepository(testDbContext1);
        var aggregate1 = SoftDeletableTestAggregate.Create("TestAggregate1");
        var aggregate2 = SoftDeletableTestAggregate.Create("TestAggregate2");
        await repository1.AddAsync(aggregate1, CancellationToken.None);
        await repository1.AddAsync(aggregate2, CancellationToken.None);
        await testDbContext1.SaveChangesAsync();
        var id1 = aggregate1.Id;
        var id2 = aggregate2.Id;
        repository1.Remove(aggregate1);
        repository1.Remove(aggregate2);
        await testDbContext1.SaveChangesAsync();
        var testDbContext2 = _sqliteInMemoryDbContextFactory.CreateContext();
        var repository2 = new SoftDeletableTestAggregateRepository(testDbContext2);
        var deletedAggregates = await repository2.GetAllDeletedAsync(CancellationToken.None);

        // Act
        repository2.PermanentlyRemoveRange(deletedAggregates);
        await testDbContext2.SaveChangesAsync();

        // Assert
        var testDbContext3 = _sqliteInMemoryDbContextFactory.CreateContext();
        var repository3 = new SoftDeletableTestAggregateRepository(testDbContext3);
        var stillDeleted = await repository3.GetAllDeletedAsync(CancellationToken.None);
        stillDeleted.Should().BeEmpty();
        var normalQuery1 = await testDbContext3.SoftDeletableTestAggregates.SingleOrDefaultAsync(e => e.Id == id1);
        var normalQuery2 = await testDbContext3.SoftDeletableTestAggregates.SingleOrDefaultAsync(e => e.Id == id2);
        normalQuery1.Should().BeNull();
        normalQuery2.Should().BeNull();
    }

    [Fact]
    public async Task SoftDeleteInterceptor_WhenEntityIsModified_ShouldUpdateModifiedAt()
    {
        // Arrange
        var testDbContext = _sqliteInMemoryDbContextFactory.CreateContext();
        var repository = new SoftDeletableTestAggregateRepository(testDbContext);
        var aggregate = SoftDeletableTestAggregate.Create("TestAggregate");
        await repository.AddAsync(aggregate, CancellationToken.None);
        await testDbContext.SaveChangesAsync();
        var initialModifiedAt = aggregate.ModifiedAt;

        // Act
        aggregate.Name = "UpdatedName";
        repository.Update(aggregate);
        await testDbContext.SaveChangesAsync();

        // Assert
        aggregate.ModifiedAt.Should().NotBe(initialModifiedAt);
    }

    [Fact]
    public async Task GetDeletedByIdAsync_WhenEntityIsNotDeleted_ShouldReturnNull()
    {
        // Arrange
        var testDbContext = _sqliteInMemoryDbContextFactory.CreateContext();
        var repository = new SoftDeletableTestAggregateRepository(testDbContext);
        var aggregate = SoftDeletableTestAggregate.Create("TestAggregate");
        await repository.AddAsync(aggregate, CancellationToken.None);
        await testDbContext.SaveChangesAsync();
        var testDbContext2 = _sqliteInMemoryDbContextFactory.CreateContext();
        var repository2 = new SoftDeletableTestAggregateRepository(testDbContext2);

        // Act
        var result = await repository2.GetDeletedByIdAsync(aggregate.Id, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenEntityIsSoftDeleted_ShouldReturnNull()
    {
        // Arrange
        var testDbContext1 = _sqliteInMemoryDbContextFactory.CreateContext();
        var repository1 = new SoftDeletableTestAggregateRepository(testDbContext1);
        var aggregate = SoftDeletableTestAggregate.Create("TestAggregate");
        await repository1.AddAsync(aggregate, CancellationToken.None);
        await testDbContext1.SaveChangesAsync();
        var aggregateId = aggregate.Id;
        repository1.Remove(aggregate);
        await testDbContext1.SaveChangesAsync();
        var testDbContext2 = _sqliteInMemoryDbContextFactory.CreateContext();

        // Act
        var result = await testDbContext2.SoftDeletableTestAggregates.SingleOrDefaultAsync(e => e.Id == aggregateId);

        // Assert
        result.Should().BeNull();
    }
}
