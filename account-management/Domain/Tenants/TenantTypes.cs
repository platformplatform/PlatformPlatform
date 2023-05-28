using JetBrains.Annotations;
using PlatformPlatform.SharedKernel.DomainCore.Identity;

namespace PlatformPlatform.AccountManagement.Domain.Tenants;

public sealed record TenantId(long Value) : StronglyTypedId<TenantId>(Value)
{
    public override string ToString()
    {
        return Value.ToString();
    }

    public static explicit operator TenantId(string value)
    {
        return new TenantId(Convert.ToInt64(value));
    }
}

public enum TenantState
{
    Trial,

    [UsedImplicitly]
    Active,

    [UsedImplicitly]
    Suspended
}