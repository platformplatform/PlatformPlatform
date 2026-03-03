using System.Collections.Immutable;
using System.Text.Json;
using Account.Features.Tenants.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedKernel.Domain;
using SharedKernel.EntityFramework;

namespace Account.Features.Users.Domain;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = JsonSerializerOptions.Default;

    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.MapStronglyTypedUuid<User, UserId>(u => u.Id);
        builder.MapStronglyTypedLongId<User, TenantId>(u => u.TenantId);
        builder
            .OwnsOne(u => u.Avatar, b => b.ToJson())
            .HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(u => u.TenantId)
            .HasPrincipalKey(t => t.Id);

        builder.Property(u => u.ExternalIdentities)
            .HasConversion(
                v => JsonSerializer.Serialize(v.ToArray(), JsonSerializerOptions),
                v => JsonSerializer.Deserialize<ImmutableArray<ExternalIdentity>>(v, JsonSerializerOptions)
            )
            .Metadata.SetValueComparer(new ValueComparer<ImmutableArray<ExternalIdentity>>(
                    (c1, c2) => c1.SequenceEqual(c2),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c
                )
            );
    }
}
