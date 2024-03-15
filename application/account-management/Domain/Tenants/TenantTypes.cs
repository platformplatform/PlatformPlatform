using PlatformPlatform.SharedKernel.DomainCore.Identity;

namespace PlatformPlatform.AccountManagement.Domain.Tenants;

[TypeConverter(typeof(StronglyTypedIdTypeConverter<string, TenantId>))]
public sealed record TenantId(string Value) : StronglyTypedId<string, TenantId>(Value)
{
    public override string ToString()
    {
        return Value;
    }

    [UsedImplicitly]
    public static bool TryParse(string? value, out TenantId? result)
    {
        if (value is { Length: >= 3 and <= 30 } && value.All(c => char.IsLower(c) || char.IsDigit(c)))
        {
            result = new TenantId(value);
            return true;
        }

        result = null;
        return false;
    }
}

[UsedImplicitly(ImplicitUseTargetFlags.Members)]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TenantState
{
    Trial,
    Active,
    Suspended
}