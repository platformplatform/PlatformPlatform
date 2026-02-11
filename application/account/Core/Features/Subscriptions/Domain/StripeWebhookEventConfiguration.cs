using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlatformPlatform.SharedKernel.EntityFramework;

namespace PlatformPlatform.Account.Features.Subscriptions.Domain;

public sealed class StripeWebhookEventConfiguration : IEntityTypeConfiguration<StripeWebhookEvent>
{
    public void Configure(EntityTypeBuilder<StripeWebhookEvent> builder)
    {
        builder.MapStronglyTypedUuid<StripeWebhookEvent, StripeWebhookEventId>(e => e.Id);
    }
}
