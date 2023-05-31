using JetBrains.Annotations;
using PlatformPlatform.SharedKernel.DomainCore.Identity;

namespace PlatformPlatform.AccountManagement.Domain.Users;

public sealed record UserId(long Value) : StronglyTypedId<UserId>(Value)
{
    public override string ToString()
    {
        return Value.ToString();
    }

    public static explicit operator UserId(string value)
    {
        return new UserId(Convert.ToInt64(value));
    }
}

[UsedImplicitly(ImplicitUseTargetFlags.Members)]
public enum UserRole
{
    TenantUser = 0,
    TenantAdmin = 1,
    TenantOwner = 2
}