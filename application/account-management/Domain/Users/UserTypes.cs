using PlatformPlatform.SharedKernel.DomainCore.Identity;

namespace PlatformPlatform.AccountManagement.Domain.Users;

[TypeConverter(typeof(StronglyTypedIdTypeConverter<string, UserId>))]
[IdPrefix("usr")]
public sealed record UserId(string Value) : StronglyTypedUlid<UserId>(Value)
{
    public override string ToString()
    {
        return Value;
    }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserRole
{
    Member,
    Admin,
    Owner
}

public enum SortableUserProperties
{
    CreatedAt,
    ModifiedAt,
    Name,
    Email,
    Role
}
