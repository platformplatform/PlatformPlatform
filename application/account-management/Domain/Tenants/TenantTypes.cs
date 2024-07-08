using PlatformPlatform.SharedKernel.DomainCore.Identity;

namespace PlatformPlatform.AccountManagement.Domain.Tenants;

[TypeConverter(typeof(StronglyTypedIdTypeConverter<string, TenantId>))]
public sealed record TenantId(string Value) : StronglyTypedId<string, TenantId>(Value)
{
    public override string ToString()
    {
        return Value;
    }

    public static bool TryParse(string? value, out TenantId? result)
    {
        if (value is not { Length: >= 3 and <= 30 })
        {
            result = null;
            return false;
        }

        if (!value.All(c => char.IsLower(c) || char.IsDigit(c) || char.IsDigit('-')))
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

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TenantState
{
    Trial,
    Active,
    Suspended
}
