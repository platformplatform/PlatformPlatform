using Account.Database;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain;
using SharedKernel.Persistence;

namespace Account.Features.FeatureFlags.Domain;

public interface IFeatureFlagRepository : ICrudRepository<FeatureFlag, FeatureFlagId>
{
    Task<FeatureFlag[]> GetAllRelevantRowsAsync(TenantId tenantId, UserId userId, CancellationToken cancellationToken);

    Task<FeatureFlag[]> GetAllBaseRowsAsync(CancellationToken cancellationToken);

    Task<FeatureFlag[]> GetTenantOverridesForFlagAsync(string featureFlagKey, CancellationToken cancellationToken);

    Task<FeatureFlag?> GetBaseRowByKeyAsync(string featureFlagKey, CancellationToken cancellationToken);

    Task<FeatureFlag?> GetByKeyAndTenantAsync(string featureFlagKey, TenantId tenantId, CancellationToken cancellationToken);

    Task<FeatureFlag?> GetByKeyAndUserAsync(string featureFlagKey, TenantId tenantId, UserId userId, CancellationToken cancellationToken);

    Task<FeatureFlag[]> GetUserOverridesForFlagAsync(string featureFlagKey, CancellationToken cancellationToken);

    Task<FeatureFlag[]> GetPlanBasedOverridesForTenantAsync(TenantId tenantId, CancellationToken cancellationToken);
}

internal sealed class FeatureFlagRepository(AccountDbContext accountDbContext)
    : RepositoryBase<FeatureFlag, FeatureFlagId>(accountDbContext), IFeatureFlagRepository
{
    public async Task<FeatureFlag[]> GetAllRelevantRowsAsync(TenantId tenantId, UserId userId, CancellationToken cancellationToken)
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

    public async Task<FeatureFlag[]> GetTenantOverridesForFlagAsync(string featureFlagKey, CancellationToken cancellationToken)
    {
        return await DbSet
            .Where(f => f.FeatureFlagKey == featureFlagKey && f.TenantId != null && f.UserId == null)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<FeatureFlag?> GetBaseRowByKeyAsync(string featureFlagKey, CancellationToken cancellationToken)
    {
        return await DbSet
            .FirstOrDefaultAsync(f => f.FeatureFlagKey == featureFlagKey && f.TenantId == null && f.UserId == null, cancellationToken);
    }

    public async Task<FeatureFlag?> GetByKeyAndTenantAsync(string featureFlagKey, TenantId tenantId, CancellationToken cancellationToken)
    {
        return await DbSet
            .FirstOrDefaultAsync(f => f.FeatureFlagKey == featureFlagKey && f.TenantId == tenantId && f.UserId == null, cancellationToken);
    }

    public async Task<FeatureFlag?> GetByKeyAndUserAsync(string featureFlagKey, TenantId tenantId, UserId userId, CancellationToken cancellationToken)
    {
        return await DbSet
            .FirstOrDefaultAsync(f => f.FeatureFlagKey == featureFlagKey && f.TenantId == tenantId && f.UserId == userId, cancellationToken);
    }

    public async Task<FeatureFlag[]> GetUserOverridesForFlagAsync(string featureFlagKey, CancellationToken cancellationToken)
    {
        return await DbSet
            .Where(f => f.FeatureFlagKey == featureFlagKey && f.UserId != null)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<FeatureFlag[]> GetPlanBasedOverridesForTenantAsync(TenantId tenantId, CancellationToken cancellationToken)
    {
        return await DbSet
            .Where(f => f.TenantId == tenantId && f.UserId == null && f.Source == FeatureFlagSource.Plan)
            .ToArrayAsync(cancellationToken);
    }
}
