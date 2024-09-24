using System.Diagnostics.CodeAnalysis;
using PlatformPlatform.SharedKernel.IdGenerators;

namespace PlatformPlatform.SharedKernel.Domain;

[PublicAPI]
[TypeConverter(typeof(StronglyTypedIdTypeConverter<string, TenantId>))]
[JsonConverter(typeof(StronglyTypedIdJsonConverter<string, TenantId>))]
public sealed record TenantId(string Value) : StronglyTypedId<string, TenantId>(Value)
{
    public override string ToString()
    {
        return Value;
    }

    public static bool TryParse(string? value, [NotNullWhen(true)] out TenantId? result)
    {
        if (value is not { Length: >= 3 and <= 30 })
        {
            result = null;
            return false;
        }

        if (!value.All(c => char.IsLower(c) || char.IsDigit(c) || c == '-'))
        {
            result = null;
            return false;
        }

        if (value.StartsWith('-') || value.EndsWith('-'))
        {
            result = null;
            return false;
        }

        result = new TenantId(value);
        return true;
    }
}
