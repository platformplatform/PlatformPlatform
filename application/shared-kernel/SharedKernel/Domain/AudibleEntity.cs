using System.ComponentModel.DataAnnotations;

namespace PlatformPlatform.SharedKernel.Domain;

/// <summary>
///     The AudibleEntity class extends Entity and implements IAuditableEntity, which adds
///     a readonly CreatedAt and private ModifiedAt properties to derived entities.
/// </summary>
public abstract class AudibleEntity<T>(T id) : Entity<T>(id), IAuditableEntity where T : IComparable<T>
{
    public DateTimeOffset CreatedAt { get; init; } = TimeProviderAccessor.Current.GetUtcNow();

    [ConcurrencyCheck]
    public DateTimeOffset? ModifiedAt { get; private set; }

    // Used by UpdateAuditableEntitiesInterceptor via explicit interface implementation
    void IAuditableEntity.UpdateModifiedAt(DateTimeOffset? modifiedAt)
    {
        ModifiedAt = modifiedAt;
    }
}
