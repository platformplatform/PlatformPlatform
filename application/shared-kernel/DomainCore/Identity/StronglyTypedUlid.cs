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
        if (value is null)
        {
            result = null;
            return false;
        }

        var prefix = GetPrefix();

        var idValue = !string.IsNullOrWhiteSpace(prefix) ? value.Replace($"{prefix}_", string.Empty) : value;

        var success = Ulid.TryParse(idValue, out var parsedValue);
        result = success ? FormUlid(parsedValue) : null;
        return success;
    }

    private static T FormUlid(Ulid newValue)
    {
        var prefix = GetPrefix();
        return (T)Activator.CreateInstance(
            typeof(T),
            BindingFlags.Instance | BindingFlags.Public,
            null,
            [!string.IsNullOrWhiteSpace(prefix) ? $"{prefix}_{newValue}" : newValue.ToString()],
            null
        )!;
    }

    private static string GetPrefix()
    {
        var idPrefixAttribute = typeof(T).GetCustomAttribute<IdPrefixAttribute>();

        return idPrefixAttribute is null
            ? throw new InvalidOperationException("The IdPrefix attribute is required for all StronglyTypeUlid objects")
            : idPrefixAttribute.Prefix;
    }
}