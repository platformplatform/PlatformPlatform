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

public sealed class UserIdTypeConverter : StronglyTypedIdTypeConverter<string, UserId>;

[UsedImplicitly(ImplicitUseTargetFlags.Members)]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserRole
{
    TenantUser,
    TenantAdmin,
    TenantOwner
}

public enum SortableUserProperties
{
    CreatedAt,
    ModifiedAt,
    Name,
    Email,
    UserRole
}