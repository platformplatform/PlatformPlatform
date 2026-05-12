using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedKernel.Domain;
using SharedKernel.EntityFramework;

namespace Account.Features.Subscriptions.Domain;

public sealed class BillingEventConfiguration : IEntityTypeConfiguration<BillingEvent>
{
    public void Configure(EntityTypeBuilder<BillingEvent> builder)
    {
        builder.MapStronglyTypedUuid<BillingEvent, BillingEventId>(e => e.Id);
        builder.MapStronglyTypedLongId<BillingEvent, TenantId>(e => e.TenantId);
        builder.MapStronglyTypedUuid<BillingEvent, SubscriptionId>(e => e.SubscriptionId);

        builder.Property(e => e.PreviousAmount).HasPrecision(18, 2);
        builder.Property(e => e.NewAmount).HasPrecision(18, 2);
        builder.Property(e => e.AmountDelta).HasPrecision(18, 2);
        builder.Property(e => e.CommittedMrr).HasPrecision(18, 2);

        builder.HasIndex(e => e.StripeEventId).IsUnique();
        builder.HasIndex(e => new { e.TenantId, e.OccurredAt }).IsDescending(false, true);
        builder.HasIndex(e => e.OccurredAt).IsDescending();
        builder.HasIndex(e => e.SubscriptionId);
    }
}
