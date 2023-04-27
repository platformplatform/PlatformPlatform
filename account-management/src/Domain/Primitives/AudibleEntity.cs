namespace PlatformPlatform.AccountManagement.Domain.Primitives;

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

    public DateTime CreatedAt { get; }

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