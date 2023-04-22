using System.Reflection;

namespace PlatformPlatform.AccountManagement.Domain.Primitives;

/// <summary>
///     This is a special version of <see cref="StronglyTypedId{T}" /> for longs which is the recommended type to use.
///     It uses the <see cref="IdGenerator" />  to create IDs that are chronological and guaranteed to be unique.
/// </summary>
public abstract record StronglyTypedId<T>(long Value) : StronglyTypedId<long, T>(Value)
    where T : StronglyTypedId<T>
{
    public static T NewId()
    {
        var newValue = IdGenerator.NewId();
        return FormLong(newValue);
    }

    public static T FromString(string value)
    {
        var newValue = Convert.ToInt64(value);
        return FormLong(newValue);
    }

    private static T FormLong(long newValue)
    {
        return (T) Activator.CreateInstance(typeof(T),
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, new object[] {newValue}, null)!;
    }
}

/// <summary>
///     StronglyTypedId is an abstract record type for creating strongly typed IDs with a specified value type.
///     It makes the code more clear and meaningful in the domain, and the type safety helps prevent bugs.
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

    /// <summary>
    ///     This returns the raw string value of the ID, this is different from the ToString() method,
    ///     which returns the type name and the value.
    /// </summary>
    public string? AsRawString()
    {
        return Value.ToString();
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