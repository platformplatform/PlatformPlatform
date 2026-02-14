using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.EntityFramework;

namespace PlatformPlatform.Account.Features.Subscriptions.Domain;

public sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = JsonSerializerOptions.Default;

    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.MapStronglyTypedUuid<Subscription, SubscriptionId>(s => s.Id);
        builder.MapStronglyTypedLongId<Subscription, TenantId>(s => s.TenantId);
        builder.MapStronglyTypedNullableId<Subscription, StripeCustomerId, string>(s => s.StripeCustomerId);
        builder.MapStronglyTypedNullableId<Subscription, StripeSubscriptionId, string>(s => s.StripeSubscriptionId);

        builder.Property(s => s.PaymentTransactions)
            .HasColumnName("PaymentTransactions")
            .HasConversion(
                v => JsonSerializer.Serialize(v.ToArray(), JsonSerializerOptions),
                v => JsonSerializer.Deserialize<ImmutableArray<PaymentTransaction>>(v, JsonSerializerOptions)
            )
            .Metadata.SetValueComparer(new ValueComparer<ImmutableArray<PaymentTransaction>>(
                    (c1, c2) => c1.SequenceEqual(c2),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c
                )
            );

        builder.OwnsOne(s => s.PaymentMethod, b => b.ToJson());

        builder.Property(s => s.BillingInfo)
            .HasColumnName("BillingInfo")
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, JsonSerializerOptions),
                v => v == null ? null : JsonSerializer.Deserialize<BillingInfo>(v, JsonSerializerOptions)
            );
    }
}
