using Microsoft.EntityFrameworkCore;
using PlatformPlatform.Foundation.DomainModeling.Persistence;

namespace PlatformPlatform.Foundation.Tests.TestEntities;

public class TestAggregateRepository : IRepository<TestAggregate, long>
{
    private readonly DbSet<TestAggregate> _testAggregatesDbSet;

    public TestAggregateRepository(TestDbContext testDbContext)
    {
        _testAggregatesDbSet = testDbContext.TestAggregates;
    }

    public async Task<TestAggregate?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        return await _testAggregatesDbSet.FindAsync(new object?[] {id}, cancellationToken);
    }

    public void Add(TestAggregate aggregate)
    {
        _testAggregatesDbSet.Add(aggregate);
    }

    public void Update(TestAggregate aggregate)
    {
        _testAggregatesDbSet.Update(aggregate);
    }

    public void Remove(TestAggregate aggregate)
    {
        _testAggregatesDbSet.Remove(aggregate);
    }
}