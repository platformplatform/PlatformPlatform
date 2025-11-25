using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace PlatformPlatform.SharedKernel.StronglyTypedIds;

/// <summary>
///     This is a strongly typed ID for string values. It uses a custom string value with an optional prefix.
///     IDs can be prefixed with the value of the <see cref="IdPrefixAttribute" /> inspired by Stripe's API.
/// </summary>
public abstract record StronglyTypedString<T>(string Value) : StronglyTypedId<string, T>(Value)
    where T : StronglyTypedString<T>
{
    public static T NewId(string value)
    {
        var prefixWithUnderscore = PrefixCache.GetPrefixWithUnderscore(typeof(T));
        if (prefixWithUnderscore is not null && !IsValidPrefixedValue(value, prefixWithUnderscore))
        {
            var prefix = PrefixCache.GetPrefix(typeof(T));
            throw new ArgumentException($"Value must start with prefix '{prefix}_' followed by at least one character", nameof(value));
        }

        return CreateInstance(value);
    }

    public static bool TryParse(string? value, [NotNullWhen(true)] out T? result)
    {
        var prefixWithUnderscore = PrefixCache.GetPrefixWithUnderscore(typeof(T));
        if (value is null || (prefixWithUnderscore is not null && !IsValidPrefixedValue(value, prefixWithUnderscore)))
        {
            result = null;
            return false;
        }

        result = CreateInstance(value);
        return true;
    }

    private static bool IsValidPrefixedValue(string value, string prefixWithUnderscore)
    {
        return value.AsSpan().StartsWith(prefixWithUnderscore, StringComparison.Ordinal)
               && value.Length > prefixWithUnderscore.Length;
    }

    private static T CreateInstance(string value)
    {
        return (T)Activator.CreateInstance(
            typeof(T),
            BindingFlags.Instance | BindingFlags.Public,
            null,
            [value],
            null
        )!;
    }
}

internal static class PrefixCache
{
    private static readonly ConcurrentDictionary<Type, string?> Prefixes = new();
    private static readonly ConcurrentDictionary<Type, string?> PrefixesWithUnderscore = new();

    public static string? GetPrefix(Type type)
    {
        return Prefixes.GetOrAdd(type, t => t.GetCustomAttribute<IdPrefixAttribute>()?.Prefix);
    }

    public static string? GetPrefixWithUnderscore(Type type)
    {
        return PrefixesWithUnderscore.GetOrAdd(type, t =>
            {
                var prefix = GetPrefix(t);
                return prefix is not null ? $"{prefix}_" : null;
            }
        );
    }
}
