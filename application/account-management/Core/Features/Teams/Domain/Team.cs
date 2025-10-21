using JetBrains.Annotations;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.StronglyTypedIds;

namespace PlatformPlatform.AccountManagement.Features.Teams.Domain;

public sealed class Team : AggregateRoot<TeamId>, ITenantScopedEntity
{
    private Team(TenantId tenantId, string name, string? description)
        : base(TeamId.NewId())
    {
        TenantId = tenantId;
        Name = name;
        Description = description ?? string.Empty;
    }

    public string Name { get; private set; }

    public string Description { get; private set; }

    public TenantId TenantId { get; }

    public static Team Create(TenantId tenantId, string name, string? description)
    {
        return new Team(tenantId, name, description);
    }

    public void Update(string name, string? description)
    {
        Name = name;
        Description = description ?? string.Empty;
    }
}

[PublicAPI]
[IdPrefix("team")]
[JsonConverter(typeof(StronglyTypedIdJsonConverter<string, TeamId>))]
public sealed record TeamId(string Value) : StronglyTypedUlid<TeamId>(Value)
{
    public override string ToString()
    {
        return Value;
    }
}
