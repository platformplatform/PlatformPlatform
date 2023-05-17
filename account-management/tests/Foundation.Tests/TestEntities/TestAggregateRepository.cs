using Microsoft.EntityFrameworkCore;
using PlatformPlatform.Foundation.Domain;

namespace PlatformPlatform.Foundation.Tests.TestEntities;

public class TestAggregateRepository : IRepository<TestAggregate, long>
{
    private readonly DbSet<TestAggregate> _dbSet;

    public TestAggregateRepository(DbContext testDbContext)
    {
        _dbSet = testDbContext.Set<TestAggregate>();
    }

    public async Task<TestAggregate?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        return await _dbSet.FindAsync(new object?[] {id}, cancellationToken);
    }

    public void Add(TestAggregate aggregate)
    {
        _dbSet.Add(aggregate);
    }

    public void Update(TestAggregate aggregate)
    {
        _dbSet.Update(aggregate);
    }

    public void Remove(TestAggregate aggregate)
    {
        _dbSet.Remove(aggregate);
    }
}