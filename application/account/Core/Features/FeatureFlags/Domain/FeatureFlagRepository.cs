using Account.Database;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain;
using SharedKernel.Persistence;

namespace Account.Features.FeatureFlags.Domain;

public interface IFeatureFlagRepository : ICrudRepository<FeatureFlag, FeatureFlagId>
{
    Task<FeatureFlag[]> GetAllRelevantFeatureFlagsAsync(TenantId tenantId, UserId userId, CancellationToken cancellationToken);

    Task<FeatureFlag[]> GetAllBaseFeatureFlagsAsync(CancellationToken cancellationToken);

    Task<FeatureFlag[]> GetTenantOverridesForFlagAsync(FeatureFlagKey featureFlagKey, CancellationToken cancellationToken);

    Task<FeatureFlag?> GetBaseFeatureFlagByKeyAsync(FeatureFlagKey featureFlagKey, CancellationToken cancellationToken);

    Task<FeatureFlag?> GetByKeyAndTenantAsync(FeatureFlagKey featureFlagKey, TenantId tenantId, CancellationToken cancellationToken);

    Task<FeatureFlag?> GetByKeyAndUserAsync(FeatureFlagKey featureFlagKey, TenantId tenantId, UserId userId, CancellationToken cancellationToken);

    Task<FeatureFlag[]> GetUserOverridesForFlagAsync(FeatureFlagKey featureFlagKey, CancellationToken cancellationToken);

    Task<FeatureFlag[]> GetPlanBasedOverridesForTenantAsync(TenantId tenantId, CancellationToken cancellationToken);
}

internal sealed class FeatureFlagRepository(AccountDbContext accountDbContext)
    : RepositoryBase<FeatureFlag, FeatureFlagId>(accountDbContext), IFeatureFlagRepository
{
    public async Task<FeatureFlag[]> GetAllRelevantFeatureFlagsAsync(TenantId tenantId, UserId userId, CancellationToken cancellationToken)
    {
        return await DbSet
            .Where(f => (f.TenantId == null || f.TenantId == tenantId) && (f.UserId == null || f.UserId == userId))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<FeatureFlag[]> GetAllBaseFeatureFlagsAsync(CancellationToken cancellationToken)
    {
        return await DbSet
            .Where(f => f.TenantId == null && f.UserId == null)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<FeatureFlag[]> GetTenantOverridesForFlagAsync(FeatureFlagKey featureFlagKey, CancellationToken cancellationToken)
    {
        return await DbSet
            .Where(f => f.FeatureFlagKey == featureFlagKey && f.TenantId != null && f.UserId == null)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<FeatureFlag?> GetBaseFeatureFlagByKeyAsync(FeatureFlagKey featureFlagKey, CancellationToken cancellationToken)
    {
        return await DbSet
            .SingleOrDefaultAsync(f => f.FeatureFlagKey == featureFlagKey && f.TenantId == null && f.UserId == null, cancellationToken);
    }

    public async Task<FeatureFlag?> GetByKeyAndTenantAsync(FeatureFlagKey featureFlagKey, TenantId tenantId, CancellationToken cancellationToken)
    {
        return await DbSet
            .SingleOrDefaultAsync(f => f.FeatureFlagKey == featureFlagKey && f.TenantId == tenantId && f.UserId == null, cancellationToken);
    }

    public async Task<FeatureFlag?> GetByKeyAndUserAsync(FeatureFlagKey featureFlagKey, TenantId tenantId, UserId userId, CancellationToken cancellationToken)
    {
        return await DbSet
            .SingleOrDefaultAsync(f => f.FeatureFlagKey == featureFlagKey && f.TenantId == tenantId && f.UserId == userId, cancellationToken);
    }

    public async Task<FeatureFlag[]> GetUserOverridesForFlagAsync(FeatureFlagKey featureFlagKey, CancellationToken cancellationToken)
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
