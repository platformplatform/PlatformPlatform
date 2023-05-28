using Microsoft.EntityFrameworkCore;
using PlatformPlatform.SharedKernel.DomainModeling.Entities;
using PlatformPlatform.SharedKernel.DomainModeling.Persistence;

namespace PlatformPlatform.SharedKernel.InfrastructureCore.Persistence;

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
        var keyValues = new object?[] {id};
        return await DbSet.FindAsync(keyValues, cancellationToken);
    }

    public async Task AddAsync(T aggregate)
    {
        if (aggregate is null) throw new ArgumentNullException(nameof(aggregate));
        await DbSet.AddAsync(aggregate);
    }

    public void Update(T aggregate)
    {
        if (aggregate is null) throw new ArgumentNullException(nameof(aggregate));
        DbSet.Update(aggregate);
    }

    public void Remove(T aggregate)
    {
        if (aggregate is null) throw new ArgumentNullException(nameof(aggregate));
        DbSet.Remove(aggregate);
    }
}