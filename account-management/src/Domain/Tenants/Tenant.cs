using PlatformPlatform.Foundation.Domain;

namespace PlatformPlatform.AccountManagement.Domain.Tenants;

public sealed record TenantId(long Value) : StronglyTypedId<TenantId>(Value);

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

        tenant.EnsureTenantInputHasBeenValidated();

        tenant.AddDomainEvent(new TenantCreatedEvent(tenant.Id, tenant.Name));

        return tenant;
    }

    private void EnsureTenantInputHasBeenValidated()
    {
        var allErrors = TenantValidation.ValidateName(Name).Errors
            .Concat(TenantValidation.ValidateSubdomain(Subdomain).Errors)
            .Concat(TenantValidation.ValidateEmail(Email).Errors)
            .Concat(TenantValidation.ValidatePhone(Phone).Errors)
            .ToArray();

        if (allErrors.Length == 0) return;

        throw new InvalidOperationException("Ensure that there is logic in place to never create an invalid tenant.");
    }

    public void Update(string tenantName, string email, string? phone)
    {
        Name = tenantName;
        Email = email;
        Phone = phone;

        EnsureTenantInputHasBeenValidated();
    }
}