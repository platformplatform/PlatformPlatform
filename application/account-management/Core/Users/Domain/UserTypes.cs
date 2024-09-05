using JetBrains.Annotations;
using PlatformPlatform.SharedKernel.IdGenerators;

namespace PlatformPlatform.AccountManagement.Core.Users.Domain;

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
