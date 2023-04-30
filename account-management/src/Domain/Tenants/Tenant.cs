using PlatformPlatform.AccountManagement.Domain.Shared;

namespace PlatformPlatform.AccountManagement.Domain.Tenants;

public sealed record TenantId(long Value) : StronglyTypedId<TenantId>(Value);

public sealed class Tenant : AudibleEntity<TenantId>, IAggregateRoot
{
    internal Tenant() : base(TenantId.NewId())
    {
        State = TenantState.Trial;
    }

    public required string Name { get; set; }

    public required string Subdomain { get; set; }

    public TenantState State { get; private set; }

    public required string Email { get; set; }

    public string? Phone { get; set; }

    public static Tenant Create(string tenantName, string subdomain, string email, string? phone)
    {
        var tenant = new Tenant {Name = tenantName, Subdomain = subdomain, Email = email, Phone = phone};

        tenant.EnsureTenantInputHasBeenValidated();

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
}