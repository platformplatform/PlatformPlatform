using Account.Database;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain;
using SharedKernel.Persistence;

namespace Account.Features.FeatureFlags.Domain;

public interface IFeatureFlagRepository : ICrudRepository<FeatureFlag, FeatureFlagId>
{
    Task<FeatureFlag[]> GetAllRelevantRowsAsync(long tenantId, string userId, CancellationToken cancellationToken);

    Task<FeatureFlag[]> GetAllBaseRowsAsync(CancellationToken cancellationToken);

    Task<FeatureFlag[]> GetTenantOverridesForFlagAsync(string flagKey, CancellationToken cancellationToken);

    Task<FeatureFlag?> GetByKeyAndScopeAsync(string flagKey, long? tenantId, string? userId, CancellationToken cancellationToken);
}

internal sealed class FeatureFlagRepository(AccountDbContext accountDbContext)
    : RepositoryBase<FeatureFlag, FeatureFlagId>(accountDbContext), IFeatureFlagRepository
{
    public async Task<FeatureFlag[]> GetAllRelevantRowsAsync(long tenantId, string userId, CancellationToken cancellationToken)
    {
        return await DbSet
            .Where(f => (f.TenantId == null || f.TenantId == tenantId) && (f.UserId == null || f.UserId == userId))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<FeatureFlag[]> GetAllBaseRowsAsync(CancellationToken cancellationToken)
    {
        return await DbSet
            .Where(f => f.TenantId == null && f.UserId == null)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<FeatureFlag[]> GetTenantOverridesForFlagAsync(string flagKey, CancellationToken cancellationToken)
    {
        return await DbSet
            .Where(f => f.FlagKey == flagKey && f.TenantId != null && f.UserId == null)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<FeatureFlag?> GetByKeyAndScopeAsync(string flagKey, long? tenantId, string? userId, CancellationToken cancellationToken)
    {
        return await DbSet
            .FirstOrDefaultAsync(f => f.FlagKey == flagKey && f.TenantId == tenantId && f.UserId == userId, cancellationToken);
    }
}
