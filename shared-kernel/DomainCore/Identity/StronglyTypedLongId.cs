using System.Reflection;
using JetBrains.Annotations;

namespace PlatformPlatform.SharedKernel.DomainCore.Identity;

/// <summary>
///     This is a special version of <see cref="StronglyTypedLongId{T}" /> for longs which is the recommended type to use.
///     It uses the <see cref="IdGenerator" />  to create IDs that are chronological and guaranteed to be unique.
/// </summary>
public abstract record StronglyTypedLongId<T>(long Value) : StronglyTypedId<long, T>(Value)
    where T : StronglyTypedLongId<T>
{
    public static T NewId()
    {
        var newValue = IdGenerator.NewId();
        return FormLong(newValue);
    }

    [UsedImplicitly]
    public static T Parse(string value)
    {
        return FormLong(long.Parse(value));
    }

    [UsedImplicitly]
    public static bool TryParse(string? value, out T? result)
    {
        var success = long.TryParse(value, out var parsedValue);
        result = success ? FormLong(parsedValue) : null;
        return success;
    }

    private static T FormLong(long newValue)
    {
        return (T) Activator.CreateInstance(typeof(T),
            BindingFlags.Instance | BindingFlags.Public, null, new object[] {newValue}, null)!;
    }
}