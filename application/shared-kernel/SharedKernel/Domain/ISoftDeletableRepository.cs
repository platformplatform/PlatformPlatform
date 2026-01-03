namespace PlatformPlatform.SharedKernel.Domain;

/// <summary>
///     Interface for repositories that manage soft-deletable aggregates.
///     Extends standard repository operations with methods for querying deleted entities,
///     restoring them, and permanently removing them from the database.
/// </summary>
public interface ISoftDeletableRepository<T, in TId> where T : IAggregateRoot, ISoftDeletable
{
    /// <summary>
    ///     Retrieves a soft-deleted entity by its ID, ignoring the soft delete filter.
    /// </summary>
    Task<T?> GetDeletedByIdAsync(TId id, CancellationToken cancellationToken);

    /// <summary>
    ///     Retrieves all soft-deleted entities, ignoring the soft delete filter.
    /// </summary>
    Task<T[]> GetAllDeletedAsync(CancellationToken cancellationToken);

    /// <summary>
    ///     Restores a soft-deleted entity by clearing its DeletedAt timestamp.
    /// </summary>
    void Restore(T aggregate);

    /// <summary>
    ///     Permanently removes an entity from the database, bypassing soft delete.
    /// </summary>
    void PermanentlyRemove(T aggregate);

    /// <summary>
    ///     Restores multiple soft-deleted entities.
    /// </summary>
    void RestoreRange(T[] aggregates);

    /// <summary>
    ///     Permanently removes multiple entities from the database.
    /// </summary>
    void PermanentlyRemoveRange(T[] aggregates);
}
