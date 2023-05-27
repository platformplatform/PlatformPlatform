using PlatformPlatform.Foundation.DomainModeling.Entities;

namespace PlatformPlatform.AccountManagement.Domain.Tenants;

public sealed class Tenant : AggregateRoot<TenantId>
{
    internal Tenant(string name, string email, string? phone) : base(TenantId.NewId())
    {
        Name = name;
        Email = email;
        Phone = phone;
        State = TenantState.Trial;
    }

    public string Name { get; private set; }

    public required string Subdomain { get; init; }

    public TenantState State { get; private set; }

    public string Email { get; private set; }

    public string? Phone { get; private set; }

    public static Tenant Create(string tenantName, string subdomain, string email, string? phone)
    {
        var tenant = new Tenant(tenantName, email, phone) {Subdomain = subdomain};
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