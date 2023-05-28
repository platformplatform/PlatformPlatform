using PlatformPlatform.Foundation.DomainModeling.Entities;

namespace PlatformPlatform.AccountManagement.Domain.Tenants;

public sealed class Tenant : AggregateRoot<TenantId>
{
    internal Tenant(string name, string subdomain, string email, string? phone) : base(TenantId.NewId())
    {
        Name = name;
        Subdomain = subdomain;
        Email = email;
        Phone = phone;
        State = TenantState.Trial;
    }

    public string Name { get; private set; }

    public string Subdomain { get; init; }

    public TenantState State { get; private set; }

    public string Email { get; private set; }

    public string? Phone { get; private set; }

    public static Tenant Create(string tenantName, string subdomain, string email, string? phone)
    {
        var tenant = new Tenant(tenantName, subdomain, email, phone);
        tenant.AddDomainEvent(new TenantCreatedEvent(tenant.Id));
        return tenant;
    }

    public void Update(string tenantName, string email, string? phone)
    {
        Name = tenantName;
        Email = email;
        Phone = phone;
    }
}