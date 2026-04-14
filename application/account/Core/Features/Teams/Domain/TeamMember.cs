using JetBrains.Annotations;
using SharedKernel.Domain;
using SharedKernel.StronglyTypedIds;

namespace Account.Features.Teams.Domain;

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

    public static TeamMember Create(TenantId tenantId, TeamId teamId, UserId userId, TeamMemberRole role = TeamMemberRole.Member)
    {
        return new TeamMember(tenantId, teamId, userId, role);
    }

    public void ChangeRole(TeamMemberRole role)
    {
        Role = role;
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
