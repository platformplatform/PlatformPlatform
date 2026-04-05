using System.Diagnostics.CodeAnalysis;
using SharedKernel.StronglyTypedIds;

namespace SharedKernel.Domain;

[PublicAPI]
[JsonConverter(typeof(StronglyTypedIdJsonConverter<string, FeatureFlagKey>))]
public sealed record FeatureFlagKey(string Value) : StronglyTypedId<string, FeatureFlagKey>(Value)
{
    public static bool TryParse(string? value, [NotNullWhen(true)] out FeatureFlagKey? result)
    {
        if (string.IsNullOrEmpty(value))
        {
            result = null;
            return false;
        }

        result = new FeatureFlagKey(value);
        return true;
    }

    public override string ToString()
    {
        return Value;
    }
}
