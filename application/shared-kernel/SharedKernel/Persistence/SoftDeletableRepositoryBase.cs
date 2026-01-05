using Microsoft.EntityFrameworkCore;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.EntityFramework;

namespace PlatformPlatform.SharedKernel.Persistence;

/// <summary>
///     Base class for repositories managing soft-deletable aggregates.
///     Extends <see cref="RepositoryBase{T,TId}" /> with operations for querying deleted entities,
///     restoring them, and permanently removing them from the database.
/// </summary>
[UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public abstract class SoftDeletableRepositoryBase<T, TId>(DbContext context)
    : RepositoryBase<T, TId>(context), ISoftDeletableRepository<T, TId>
    where T : AggregateRoot<TId>, ISoftDeletable where TId : IComparable<TId>
{
    /// <summary>
    ///     Retrieves a soft-deleted entity by its ID, ignoring the soft delete filter.
    ///     The tenant filter is still applied for tenant-scoped entities.
    /// </summary>
    public async Task<T?> GetDeletedByIdAsync(TId id, CancellationToken cancellationToken)
    {
        return await DbSet
            .IgnoreQueryFilters([QueryFilterNames.SoftDelete])
            .Where(e => e.DeletedAt != null)
            .SingleOrDefaultAsync(e => e.Id.Equals(id), cancellationToken);
    }

    /// <summary>
    ///     Retrieves all soft-deleted entities, ignoring the soft delete filter.
    ///     The tenant filter is still applied for tenant-scoped entities.
    /// </summary>
    public async Task<T[]> GetAllDeletedAsync(CancellationToken cancellationToken)
    {
        return await DbSet
            .IgnoreQueryFilters([QueryFilterNames.SoftDelete])
            .Where(e => e.DeletedAt != null)
            .ToArrayAsync(cancellationToken);
    }

    /// <summary>
    ///     Restores a soft-deleted entity by clearing its DeletedAt timestamp.
    /// </summary>
    public void Restore(T aggregate)
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        aggregate.Restore();
        Update(aggregate);
    }

    /// <summary>
    ///     Permanently removes (purges) an entity from the database, bypassing soft delete.
    /// </summary>
    public void PermanentlyRemove(T aggregate)
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        aggregate.MarkForPurge();
        Remove(aggregate);
    }

    /// <summary>
    ///     Restores multiple soft-deleted entities.
    /// </summary>
    public void RestoreRange(T[] aggregates)
    {
        foreach (var aggregate in aggregates)
        {
            aggregate.Restore();
        }

        UpdateRange(aggregates);
    }

    /// <summary>
    ///     Permanently removes (purges) multiple entities from the database.
    /// </summary>
    public void PermanentlyRemoveRange(T[] aggregates)
    {
        foreach (var aggregate in aggregates)
        {
            aggregate.MarkForPurge();
        }

        RemoveRange(aggregates);
    }
}
