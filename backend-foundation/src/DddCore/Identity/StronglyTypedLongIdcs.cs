using System.Reflection;

namespace PlatformPlatform.Foundation.DddCore.Identity;

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
            BindingFlags.Instance | BindingFlags.Public, null, new object[] {newValue}, null)!;
    }
}