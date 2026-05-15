using Account.Features.Users.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedKernel.Domain;
using SharedKernel.EntityFramework;

namespace Account.Features.FeatureFlags.Domain;

public sealed class FeatureFlagConfiguration : IEntityTypeConfiguration<FeatureFlag>
{
    public void Configure(EntityTypeBuilder<FeatureFlag> builder)
    {
        builder.MapStronglyTypedUuid<FeatureFlag, FeatureFlagId>(f => f.Id);
        builder.MapStronglyTypedNullableLongId<FeatureFlag, TenantId>(f => f.TenantId);
        builder.MapStronglyTypedNullableId<FeatureFlag, UserId, string>(f => f.UserId);

        // Cascade deletes user-scoped override rows when the user is hard-deleted (purge / bulk purge /
        // recycle-bin empty). Without this, override rows would survive as orphans referencing a missing user.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
