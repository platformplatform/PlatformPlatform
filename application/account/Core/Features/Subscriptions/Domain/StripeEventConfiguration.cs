using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.EntityFramework;

namespace PlatformPlatform.Account.Features.Subscriptions.Domain;

public sealed class StripeEventConfiguration : IEntityTypeConfiguration<StripeEvent>
{
    public void Configure(EntityTypeBuilder<StripeEvent> builder)
    {
        builder.MapStronglyTypedString(e => e.Id);
        builder.MapStronglyTypedNullableId<StripeEvent, StripeCustomerId, string>(e => e.StripeCustomerId);
        builder.MapStronglyTypedNullableId<StripeEvent, StripeSubscriptionId, string>(e => e.StripeSubscriptionId);
        builder.MapStronglyTypedNullableLongId<StripeEvent, TenantId>(e => e.TenantId);
    }
}
