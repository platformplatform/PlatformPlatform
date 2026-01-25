using JetBrains.Annotations;

namespace PlatformPlatform.Account.Features.Users.Domain;

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
    LastSeenAt,
    Name,
    Email,
    Role
}

[PublicAPI]
public enum UserPurgeReason
{
    SingleUserPurge,
    BulkUserPurge,
    RecycleBinPurge
}
