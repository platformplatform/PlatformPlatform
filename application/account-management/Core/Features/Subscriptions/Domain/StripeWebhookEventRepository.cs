using Microsoft.EntityFrameworkCore;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.Persistence;

namespace PlatformPlatform.AccountManagement.Features.Subscriptions.Domain;

public interface IStripeWebhookEventRepository : IAppendRepository<StripeWebhookEvent, StripeWebhookEventId>
{
    Task<bool> ExistsByStripeEventIdAsync(string stripeEventId, CancellationToken cancellationToken);
}

internal sealed class StripeWebhookEventRepository(AccountManagementDbContext accountManagementDbContext)
    : RepositoryBase<StripeWebhookEvent, StripeWebhookEventId>(accountManagementDbContext), IStripeWebhookEventRepository
{
    public async Task<bool> ExistsByStripeEventIdAsync(string stripeEventId, CancellationToken cancellationToken)
    {
        return await DbSet.AnyAsync(e => e.StripeEventId == stripeEventId, cancellationToken);
    }
}
