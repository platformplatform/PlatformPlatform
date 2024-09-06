using JetBrains.Annotations;
using PlatformPlatform.SharedKernel.IdGenerators;

namespace PlatformPlatform.AccountManagement.Users.Domain;

[PublicAPI]
[IdPrefix("usr")]
[TypeConverter(typeof(StronglyTypedIdTypeConverter<string, UserId>))]
public sealed record UserId(string Value) : StronglyTypedUlid<UserId>(Value)
{
    public override string ToString()
    {
        return Value;
    }
}

[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserRole
{
    Member,
    Admin,
    Owner
}

[PublicAPI]
public enum SortableUserProperties
{
    CreatedAt,
    ModifiedAt,
    Name,
    Email,
    Role
}
