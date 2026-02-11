using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlatformPlatform.AccountManagement.Features.Tenants.Domain;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.EntityFramework;

namespace PlatformPlatform.AccountManagement.Features.Users.Domain;

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
            .HasColumnName("ExternalIdentities")
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
