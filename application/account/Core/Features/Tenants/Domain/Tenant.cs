using PlatformPlatform.SharedKernel.Domain;

namespace PlatformPlatform.Account.Features.Tenants.Domain;

public sealed class Tenant : AggregateRoot<TenantId>
{
    private Tenant() : base(TenantId.NewId())
    {
        State = TenantState.Active;
        Logo = new Logo();
    }

    public string Name { get; private set; } = string.Empty;

    public TenantState State { get; private set; }

    public Logo Logo { get; private set; }

    public static Tenant Create(string email)
    {
        var tenant = new Tenant();
        tenant.AddDomainEvent(new TenantCreatedEvent(tenant.Id, email));
        return tenant;
    }

    public void SetState(TenantState state)
    {
        State = state;
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
