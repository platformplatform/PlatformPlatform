using System.ComponentModel.DataAnnotations.Schema;

namespace PlatformPlatform.AccountManagement.Domain.Primitives;

[DebuggerDisplay("Identity = {" + nameof(Id) + "}")]
public abstract class Entity<T> : IEquatable<Entity<T>> where T : IComparable<T>
{
    protected Entity(T id)
    {
        Id = id;
    }

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