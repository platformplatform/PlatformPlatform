namespace PlatformPlatform.SharedKernel.DomainModeling.Entities;

public sealed class EntityEqualityComparer<T> : IEqualityComparer<Entity<T>> where T : IComparable<T>
{
    public bool Equals(Entity<T>? x, Entity<T>? y)
    {
        return ReferenceEquals(x, y) ||
               (!ReferenceEquals(x, null) &&
                !ReferenceEquals(y, null) &&
                EqualityComparer<T>.Default.Equals(x.Id, y.Id)
               );
    }

    public int GetHashCode(Entity<T> obj)
    {
        return EqualityComparer<T>.Default.GetHashCode(obj.Id);
    }
}