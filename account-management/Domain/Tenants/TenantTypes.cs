using System.ComponentModel;
using JetBrains.Annotations;
using PlatformPlatform.SharedKernel.DomainCore.Identity;

namespace PlatformPlatform.AccountManagement.Domain.Tenants;

[TypeConverter(typeof(TenantIdTypeConverter))]
public sealed record TenantId(long Value) : StronglyTypedId<TenantId>(Value)
{
    public override string ToString()
    {
        return Value.ToString();
    }
}

public sealed class TenantIdTypeConverter : StronglyTypedIdTypeConverter<TenantId>
{
}

[UsedImplicitly(ImplicitUseTargetFlags.Members)]
public enum TenantState
{
    Trial,
    Active,
    Suspended
}