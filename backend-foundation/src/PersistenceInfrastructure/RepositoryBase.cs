using Microsoft.EntityFrameworkCore;
using PlatformPlatform.Foundation.DomainModeling.Entities;
using PlatformPlatform.Foundation.DomainModeling.Persistence;

namespace PlatformPlatform.Foundation.PersistenceInfrastructure;

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

    public void Add(T entity)
    {
        DbSet.Add(entity);
    }

    public void Update(T entity)
    {
        DbSet.Update(entity);
    }

    public void Remove(T entity)
    {
        DbSet.Remove(entity);
    }
}