using JetBrains.Annotations;

namespace Account.Features.Teams.Domain;

[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TeamMemberRole
{
    Member,
    Admin
}
