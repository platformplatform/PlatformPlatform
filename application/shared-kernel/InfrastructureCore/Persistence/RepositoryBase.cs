using Microsoft.EntityFrameworkCore;
using PlatformPlatform.SharedKernel.DomainCore.Entities;
using PlatformPlatform.SharedKernel.DomainCore.Persistence;

namespace PlatformPlatform.SharedKernel.InfrastructureCore.Persistence;

/// <summary>
///     RepositoryBase contains implementations for generic repository features. Repositories are a DDD concept, and are
///     used as an abstraction over the database. In DDD, repositories are used to persist <see cref="IAggregateRoot" />.
///     <see cref="Entity{T}" /> that are not aggregates cannot be fetched from the database using a repository. They
///     must be fetched indirectly by fetching the aggregate that they belong to. E.g., to fetch an OrderLine entity,
///     you must do it by fetching the Order aggregate that it belongs to.
///     Repositories are not responsible for commiting the changes to the database, which is handled by the
///     <see cref="IUnitOfWork" />. What this means is that when you add, update, or delete aggregates, they are just
///     marked to be added, updated, or deleted, and it's not until the <see cref="IUnitOfWork" /> is committed that the
///     changes are actually persisted to the database.
/// </summary>
public abstract class RepositoryBase<T, TId>
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
        var keyValues = new object?[] { id };
        return await DbSet.FindAsync(keyValues, cancellationToken);
    }

    public async Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken)
    {
        return DbSet.Local.Any(e => e.Id.Equals(id)) || await DbSet.AnyAsync(e => e.Id.Equals(id), cancellationToken);
    }

    public async Task AddAsync(T aggregate, CancellationToken cancellationToken)
    {
        if (aggregate is null) throw new ArgumentNullException(nameof(aggregate));
        await DbSet.AddAsync(aggregate, cancellationToken);
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