using NUlid;

namespace PlatformPlatform.SharedKernel.DomainCore.Identity;

/// <summary>
///     This is a special version of <see cref="StronglyTypedLongId{T}" /> for longs which is the recommended type to use.
///     It uses the <see cref="IdGenerator" />  to create IDs that are chronological and guaranteed to be unique.
/// </summary>
public abstract record StronglyTypedUlid<T>(string Value) : StronglyTypedId<string, T>(Value)
    where T : StronglyTypedUlid<T>
{
    public static T NewId()
    {
        var newValue = Ulid.NewUlid();
        return FormUlid(newValue);
    }

    [UsedImplicitly]
    public static bool TryParse(string? value, out T? result)
    {
        var success = Ulid.TryParse(value ?? "", out var parsedValue);
        result = success ? FormUlid(parsedValue) : null;
        return success;
    }

    private static T FormUlid(Ulid newValue)
    {
        return (T)Activator.CreateInstance(typeof(T),
            BindingFlags.Instance | BindingFlags.Public,
            null,
            new object[] { newValue.ToString() },
            null
        )!;
    }
}