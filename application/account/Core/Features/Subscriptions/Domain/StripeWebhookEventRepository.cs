using Microsoft.EntityFrameworkCore;
using PlatformPlatform.Account.Database;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.Persistence;

namespace PlatformPlatform.Account.Features.Subscriptions.Domain;

public interface IStripeWebhookEventRepository : IAppendRepository<StripeWebhookEvent, StripeWebhookEventId>
{
    Task<bool> ExistsByStripeEventIdAsync(string stripeEventId, CancellationToken cancellationToken);
}

internal sealed class StripeWebhookEventRepository(AccountDbContext accountDbContext)
    : RepositoryBase<StripeWebhookEvent, StripeWebhookEventId>(accountDbContext), IStripeWebhookEventRepository
{
    public async Task<bool> ExistsByStripeEventIdAsync(string stripeEventId, CancellationToken cancellationToken)
    {
        return await DbSet.AnyAsync(e => e.StripeEventId == stripeEventId, cancellationToken);
    }
}
