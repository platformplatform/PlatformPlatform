using System.ComponentModel.DataAnnotations;
using PlatformPlatform.AccountManagement.Domain.Primitives;

namespace PlatformPlatform.AccountManagement.Domain.Tenants;

public sealed record TenantId(long Value) : StronglyTypedId<TenantId>(Value);

public sealed class Tenant : AudibleEntity<TenantId>, IAggregateRoot
{
    public Tenant() : base(TenantId.NewId())
    {
    }

    [MinLength(1)] [MaxLength(50)] public required string Name { get; set; }
}