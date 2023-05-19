using Microsoft.EntityFrameworkCore;
using PlatformPlatform.Foundation.DomainModeling.Entities;
using PlatformPlatform.Foundation.DomainModeling.Persistence;

namespace PlatformPlatform.Foundation.InfrastructureCore.Persistence;

public abstract class RepositoryBase<T, TId> : IRepository<T, TId>
    where T : AggregateRoot<TId>
    where TId : IComparable<TId>
{
    protected readonly DbSet<T> DbSet;

    protected RepositoryBase(DbContext context)
    {
        DbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(TId id, CancellationToken cancellationToken)
    {
        return await DbSet.FindAsync(new object?[] {id}, cancellationToken);
    }

    public void Add(T aggregate)
    {
        DbSet.Add(aggregate);
    }

    public void Update(T aggregate)
    {
        DbSet.Update(aggregate);
    }

    public void Remove(T aggregate)
    {
        DbSet.Remove(aggregate);
    }
}