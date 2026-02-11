using Microsoft.EntityFrameworkCore;
using PlatformPlatform.Account.Database;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Persistence;

namespace PlatformPlatform.Account.Features.Subscriptions.Domain;

public interface ISubscriptionRepository : ICrudRepository<Subscription, SubscriptionId>
{
    Task<Subscription?> GetByTenantIdAsync(CancellationToken cancellationToken);
}

internal sealed class SubscriptionRepository(AccountDbContext accountManagementDbContext, IExecutionContext executionContext)
    : RepositoryBase<Subscription, SubscriptionId>(accountManagementDbContext), ISubscriptionRepository
{
    public async Task<Subscription?> GetByTenantIdAsync(CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(executionContext.TenantId);
        return await DbSet.FirstOrDefaultAsync(s => s.TenantId == executionContext.TenantId, cancellationToken);
    }
}
