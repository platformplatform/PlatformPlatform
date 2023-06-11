using PlatformPlatform.SharedKernel.DomainCore.Entities;

namespace PlatformPlatform.AccountManagement.Domain.Tenants;

public sealed class Tenant : AggregateRoot<TenantId>
{
    internal Tenant(TenantId id, string name, string email, string? phone) : base(id)
    {
        Name = name;
        Email = email;
        Phone = phone;
        State = TenantState.Trial;
    }

    public string Name { get; private set; }

    public TenantState State { get; private set; }

    public string Email { get; private set; }

    public string? Phone { get; private set; }

    public static Tenant Create(string subdomain, string tenantName, string email, string? phone)
    {
        var tenant = new Tenant(new TenantId(subdomain), tenantName, email, phone);
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