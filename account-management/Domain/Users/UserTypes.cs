using System.ComponentModel;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using PlatformPlatform.SharedKernel.DomainCore.Identity;

namespace PlatformPlatform.AccountManagement.Domain.Users;

[TypeConverter(typeof(UserIdTypeConverter))]
[UsedImplicitly]
public sealed record UserId(string Value) : StronglyTypedUlid<UserId>(Value)
{
    public override string ToString()
    {
        return Value;
    }
}

public sealed class UserIdTypeConverter : StronglyTypedIdTypeConverter<string, UserId>
{
}

[UsedImplicitly(ImplicitUseTargetFlags.Members)]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserRole
{
    TenantUser = 0,
    TenantAdmin = 1,
    TenantOwner = 2
}