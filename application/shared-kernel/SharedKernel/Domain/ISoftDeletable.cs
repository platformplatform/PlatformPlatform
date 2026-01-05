using System.ComponentModel.DataAnnotations.Schema;

namespace PlatformPlatform.SharedKernel.Domain;

/// <summary>
///     Interface for entities that support soft delete functionality.
///     Entities implementing this interface will be filtered from queries by default
///     and can be restored or permanently deleted.
/// </summary>
public interface ISoftDeletable
{
    /// <summary>
    ///     The timestamp when the entity was soft-deleted, or null if not deleted.
    /// </summary>
    DateTimeOffset? DeletedAt { get; }

    /// <summary>
    ///     When true, the soft delete interceptor will not convert the delete to a soft delete,
    ///     allowing the entity to be permanently removed (purged) from the database.
    /// </summary>
    [NotMapped]
    bool ForcePurge { get; }

    /// <summary>
    ///     Marks the entity as deleted by setting the DeletedAt timestamp.
    /// </summary>
    void MarkAsDeleted(DateTimeOffset deletedAt);

    /// <summary>
    ///     Restores a soft-deleted entity by clearing the DeletedAt timestamp.
    /// </summary>
    void Restore();

    /// <summary>
    ///     Marks the entity for permanent deletion (purge), bypassing the soft delete interceptor.
    /// </summary>
    void MarkForPurge();
}
