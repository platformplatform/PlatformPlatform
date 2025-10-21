using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Features.Teams.Domain;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.StronglyTypedIds;

namespace PlatformPlatform.AccountManagement.Features.TeamMembers.Domain;

public sealed class TeamMember : AggregateRoot<TeamMemberId>, ITenantScopedEntity
{
    private TeamMember(TenantId tenantId, TeamId teamId, UserId userId, TeamMemberRole role)
        : base(TeamMemberId.NewId())
    {
        TenantId = tenantId;
        TeamId = teamId;
        UserId = userId;
        Role = role;
    }

    public TeamId TeamId { get; }

    public UserId UserId { get; }

    public TeamMemberRole Role { get; private set; }

    public TenantId TenantId { get; }

    public static TeamMember Create(TenantId tenantId, TeamId teamId, UserId userId, TeamMemberRole role)
    {
        return new TeamMember(tenantId, teamId, userId, role);
    }

    public void ChangeRole(TeamMemberRole newRole)
    {
        Role = newRole;
    }
}

[PublicAPI]
[IdPrefix("tmbr")]
[JsonConverter(typeof(StronglyTypedIdJsonConverter<string, TeamMemberId>))]
public sealed record TeamMemberId(string Value) : StronglyTypedUlid<TeamMemberId>(Value)
{
    public override string ToString()
    {
        return Value;
    }
}

[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TeamMemberRole
{
    Member,
    Admin
}
