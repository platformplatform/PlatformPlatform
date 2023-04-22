using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;

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

/// <summary>
///     The Entity class is a base class for entities which represents business objects.
///     Entities are a DDD concept, where an entity is a business object that has a unique identity.
///     If two entities have the same identity, they are considered to be the same entity.
///     It is recommended to use a <see cref="StronglyTypedId{T}" /> for the ID to make the domain more meaningful.
/// </summary>
[DebuggerDisplay("Identity = {" + nameof(Id) + "}")]
public abstract class Entity<T> : IEquatable<Entity<T>> where T : IComparable<T>
{
    protected Entity(T id)
    {
        Id = id;
    }

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public T Id { get; init; }

    public bool Equals(Entity<T>? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return EqualityComparer<T>.Default.Equals(Id, other.Id);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((Entity<T>) obj);
    }

    public override int GetHashCode()
    {
        return EqualityComparer<T>.Default.GetHashCode(Id);
    }

    public static bool operator ==(Entity<T>? a, Entity<T>? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;
        return a.Equals(b);
    }

    public static bool operator !=(Entity<T>? a, Entity<T>? b)
    {
        return !(a == b);
    }
}