namespace PlatformPlatform.SharedKernel.Domain;

/// <summary>
///     Marker interface for entities that support soft delete functionality.
///     Entities implementing this interface will be filtered from queries by default
///     and can be restored or permanently deleted through their repository.
/// </summary>
public interface ISoftDeletable
{
    /// <summary>
    ///     The timestamp when the entity was soft-deleted, or null if not deleted.
    /// </summary>
    DateTimeOffset? DeletedAt { get; }
}
