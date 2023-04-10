using System.ComponentModel.DataAnnotations;

namespace PlatformPlatform.AccountManagement.Domain.Tenants;

public sealed record TenantId(long Value) : IComparable<TenantId>
{
    public int CompareTo(TenantId? other)
    {
        return other == null ? 1 : Value.CompareTo(other.Value);
    }

    public static TenantId NewId()
    {
        return new TenantId(IdGenerator.NewId());
    }
}

public sealed class Tenant : Entity<TenantId>, IAggregateRoot
{
    public Tenant() : base(new TenantId(IdGenerator.NewId()))
    {
    }

    [MinLength(1)] [MaxLength(50)] public required string Name { get; set; }
}