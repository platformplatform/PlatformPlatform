using NUlid;

namespace PlatformPlatform.SharedKernel.DomainCore.Identity;

/// <summary>
///     This is the recommended ID type to use. It uses the <see cref="Ulid" /> to create unique chronological IDs.
///     IDs are prefixed with the value of the <see cref="IdPrefixAttribute" /> inspired by Stripe's API.
/// </summary>
public abstract record StronglyTypedUlid<T>(string Value) : StronglyTypedId<string, T>(Value)
    where T : StronglyTypedUlid<T>
{
    private static readonly string Prefix = typeof(T).GetCustomAttribute<IdPrefixAttribute>()?.Prefix
                                            ?? throw new InvalidOperationException("IdPrefixAttribute is required.");
    
    public static T NewId()
    {
        var newValue = Ulid.NewUlid();
        return FormUlid(newValue);
    }
    
    public static bool TryParse(string? value, out T? result)
    {
        if (value is null || !value.StartsWith($"{Prefix}_"))
        {
            result = null;
            return false;
        }
        
        if (!Ulid.TryParse(value.Replace($"{Prefix}_", ""), out var parsedValue))
        {
            result = null;
            return false;
        }
        
        result = FormUlid(parsedValue);
        return true;
    }
    
    private static T FormUlid(Ulid newValue)
    {
        return (T)Activator.CreateInstance(
            typeof(T),
            BindingFlags.Instance | BindingFlags.Public,
            null,
            [$"{Prefix}_{newValue}"],
            null
        )!;
    }
}
