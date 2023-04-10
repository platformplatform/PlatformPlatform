using System.ComponentModel.DataAnnotations;

namespace PlatformPlatform.AccountManagement.Domain.Tenants;

public sealed record TenantId(long Value) : StronglyTypedId<TenantId>(Value);

public sealed class Tenant : Entity<TenantId>, IAggregateRoot
{
    public Tenant() : base(TenantId.NewId())
    {
    }

    [MinLength(1)] [MaxLength(50)] public required string Name { get; set; }
}