using PlatformPlatform.SharedKernel.Domain;

namespace PlatformPlatform.AccountManagement.Features.Tenants.Domain;

public sealed class Tenant : AggregateRoot<TenantId>
{
    private Tenant() : base(TenantId.NewId())
    {
        State = TenantState.Trial;
    }

    public string Name { get; private set; } = string.Empty;

    public TenantState State { get; private set; }

    public static Tenant Create(string email)
    {
        var tenant = new Tenant();
        tenant.AddDomainEvent(new TenantCreatedEvent(tenant.Id, email));
        return tenant;
    }

    public void Update(string tenantName)
    {
        Name = tenantName;
    }
}
