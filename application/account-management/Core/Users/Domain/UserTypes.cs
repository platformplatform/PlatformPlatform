using JetBrains.Annotations;

namespace PlatformPlatform.AccountManagement.Users.Domain;

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
