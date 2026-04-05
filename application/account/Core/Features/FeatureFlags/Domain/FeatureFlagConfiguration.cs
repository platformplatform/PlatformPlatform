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
        builder.MapStronglyTypedId<FeatureFlag, FeatureFlagKey, string>(f => f.FeatureFlagKey);
        builder.MapStronglyTypedNullableLongId<FeatureFlag, TenantId>(f => f.TenantId);
        builder.MapStronglyTypedNullableId<FeatureFlag, UserId, string>(f => f.UserId);
    }
}
