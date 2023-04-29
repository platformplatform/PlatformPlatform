using System.ComponentModel.DataAnnotations;
using PlatformPlatform.AccountManagement.Domain.Shared;

namespace PlatformPlatform.AccountManagement.Domain.Tenants;

public sealed record TenantId(long Value) : StronglyTypedId<TenantId>(Value);

public sealed class Tenant : AudibleEntity<TenantId>, IAggregateRoot
{
    public Tenant() : base(TenantId.NewId())
    {
        State = TenantState.Trial;
    }

    [MinLength(TenantValidationConstants.NameMinLength)]
    [MaxLength(TenantValidationConstants.NameMaxLength)]
    public required string Name { get; set; }

    [MinLength(TenantValidationConstants.SubdomainMinLength)]
    [MaxLength(TenantValidationConstants.SubdomainMaxLength)]
    public required string Subdomain { get; set; }

    public TenantState State { get; private set; }

    [MaxLength(TenantValidationConstants.EmailMaxLength)]
    public required string Email { get; set; }

    [MaxLength(TenantValidationConstants.PhoneMaxLength)]
    public string? Phone { get; set; }
}