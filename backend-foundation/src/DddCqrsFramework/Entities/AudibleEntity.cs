using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace PlatformPlatform.Foundation.DddCqrsFramework.Entities;

/// <summary>
///     The AudibleEntity class extends Entity and implements IAuditableEntity, which adds
///     a readonly CreatedAt and private ModifiedAt properties to derived entities.
/// </summary>
public abstract class AudibleEntity<T> : Entity<T>, IAuditableEntity where T : IComparable<T>
{
    protected AudibleEntity(T id) : base(id)
    {
        CreatedAt = DateTime.UtcNow;
    }

    [UsedImplicitly]
    public DateTime CreatedAt { get; private set; }

    [ConcurrencyCheck]
    public DateTime? ModifiedAt { get; private set; }

    /// <summary>
    ///     This method is used by the UpdateAuditableEntitiesInterceptor in the Infrastructure layer.
    ///     It's not intended to be used by the application, which is why it is implemented using an explicit interface.
    /// </summary>
    void IAuditableEntity.UpdateModifiedAt(DateTime? modifiedAt)
    {
        ModifiedAt = modifiedAt;
    }
}