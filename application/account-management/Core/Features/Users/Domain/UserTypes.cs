using JetBrains.Annotations;

namespace PlatformPlatform.AccountManagement.Features.Users.Domain;

[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserRole
{
    Member,
    Admin,
    Owner
}

[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserStatus
{
    Active,
    Pending
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
