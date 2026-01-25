using System.Collections.Immutable;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
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

        builder.Property(s => s.PaymentTransactions)
            .HasColumnName("PaymentTransactions")
            .HasConversion(
                v => JsonSerializer.Serialize(v.ToArray(), JsonSerializerOptions),
                v => JsonSerializer.Deserialize<ImmutableArray<PaymentTransaction>>(v, JsonSerializerOptions)
            );
    }
}
