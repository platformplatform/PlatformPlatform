using System.ComponentModel.DataAnnotations;
using PlatformPlatform.AccountManagement.Domain.Primitives;

namespace PlatformPlatform.AccountManagement.Domain.Tenants;

public sealed record TenantId(long Value) : StronglyTypedId<TenantId>(Value);

public sealed class Tenant : AudibleEntity<TenantId>, IAggregateRoot
{
    public Tenant() : base(TenantId.NewId())
    {
        State = TenantState.Trial;
    }

    [MinLength(1)]
    [MaxLength(30)]
    public required string Name { get; set; }

    [MinLength(3)]
    [MaxLength(30)]
    public required string Subdomain { get; set; }

    [Required]
    public TenantState State { get; private set; }

    [EmailAddress]
    [MaxLength(100)]
    public required string Email { get; set; }

    [Phone]
    [MaxLength(20)]
    public required string Phone { get; set; }
}