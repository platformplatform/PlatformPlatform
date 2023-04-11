using System.Reflection;

namespace PlatformPlatform.AccountManagement.Domain.Primitives;

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