using JetBrains.Annotations;

namespace Account.Features.Users.Domain;

[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserRole
{
    Member,
    Admin,
    Owner
}

[PublicAPI]
public enum UserPurgeReason
{
    SingleUserPurge,
    BulkUserPurge,
    RecycleBinPurge
}
