using PlatformPlatform.SharedKernel.Domain;

namespace PlatformPlatform.Account.Features.Tenants.Domain;

public sealed class Tenant : SoftDeletableAggregateRoot<TenantId>
{
    private Tenant() : base(TenantId.NewId())
    {
        State = TenantState.Active;
        Logo = new Logo();
    }

    public string Name { get; private set; } = string.Empty;

    public TenantState State { get; private set; }

    public SuspensionReason? SuspensionReason { get; private set; }

    public DateTimeOffset? SuspendedAt { get; private set; }

    public Logo Logo { get; private set; }

    public static Tenant Create(string email)
    {
        var tenant = new Tenant();
        tenant.AddDomainEvent(new TenantCreatedEvent(tenant.Id, email));
        return tenant;
    }

    public void Suspend(SuspensionReason reason, DateTimeOffset suspendedAt)
    {
        State = TenantState.Suspended;
        SuspensionReason = reason;
        SuspendedAt = suspendedAt;
    }

    public void Activate()
    {
        State = TenantState.Active;
        SuspensionReason = null;
        SuspendedAt = null;
    }

    public void Update(string tenantName)
    {
        Name = tenantName;
    }

    public void UpdateLogo(string logoUrl)
    {
        Logo = new Logo(logoUrl, Logo.Version + 1);
    }

    public void RemoveLogo()
    {
        Logo = new Logo(Version: Logo.Version);
    }
}

public sealed record Logo(string? Url = null, int Version = 0);
