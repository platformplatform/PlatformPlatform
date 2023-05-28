namespace PlatformPlatform.SharedKernel.DomainModeling.Identity;

/// <summary>
///     StronglyTypedId is an abstract record type for creating strongly typed IDs with a specified value type. It makes
///     the code clearer and more meaningful in the domain, and the type safety helps prevent bugs. E.g., a method like
///     AddToOrder(CustomerId customerId, OrderId orderId, ProductId productId, int quantity) is clearer and provides
///     better type safety than AddToOrder(long customerId, long orderId, long productId, int quantity).
///     When used with Entity Framework, make sure to register the type in the OnModelCreating method in the DbContext.
/// </summary>
public abstract record StronglyTypedId<TValue, T>(TValue Value) : IComparable<StronglyTypedId<TValue, T>>
    where T : StronglyTypedId<TValue, T>
    where TValue : IComparable<TValue>
{
    public int CompareTo(StronglyTypedId<TValue, T>? other)
    {
        return other == null ? 1 : Value.CompareTo(other.Value);
    }

    public virtual bool Equals(StronglyTypedId<TValue, T>? other)
    {
        return other != null && Value.Equals(other.Value);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public static implicit operator TValue(StronglyTypedId<TValue, T> stronglyTypedId)
    {
        return stronglyTypedId.Value;
    }
}