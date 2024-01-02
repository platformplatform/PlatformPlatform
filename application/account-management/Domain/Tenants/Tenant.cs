using PlatformPlatform.SharedKernel.DomainCore.Entities;

namespace PlatformPlatform.AccountManagement.Domain.Tenants;

public sealed class Tenant : AggregateRoot<TenantId>
{
    private Tenant(TenantId id, string name, string? phone) : base(id)
    {
        Name = name;
        Phone = phone;
        State = TenantState.Trial;
    }

    public string Name { get; private set; }

    public TenantState State { get; private set; }

    public string? Phone { get; private set; }

    public static Tenant Create(string subdomain, string tenantName, string? phone, string email)
    {
        var tenant = new Tenant(new TenantId(subdomain), tenantName, phone);
        tenant.AddDomainEvent(new TenantCreatedEvent(tenant.Id, email));
        return tenant;
    }

    public void Update(string tenantName, string? phone)
    {
        Name = tenantName;
        Phone = phone;
    }
}