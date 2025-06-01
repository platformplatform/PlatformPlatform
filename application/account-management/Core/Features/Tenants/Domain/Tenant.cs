using PlatformPlatform.SharedKernel.Domain;

namespace PlatformPlatform.AccountManagement.Features.Tenants.Domain;

public sealed record Address(string? Street, string? City, string? Zip, string? State, string? Country)
{
    public Address() : this(null, null, null, null, null)
    {
    }
}

public sealed class Tenant : AggregateRoot<TenantId>
{
    private Tenant() : base(TenantId.NewId())
    {
        State = TenantState.Trial;
    }

    public string Name { get; private set; } = string.Empty;

    public TenantState State { get; private set; }

    public Address? Address { get; private set; }

    public static Tenant Create(string email)
    {
        var tenant = new Tenant();
        tenant.AddDomainEvent(new TenantCreatedEvent(tenant.Id, email));
        return tenant;
    }

    public void Update(string tenantName, Address? address)
    {
        Name = tenantName;
        Address = address;
    }
}
