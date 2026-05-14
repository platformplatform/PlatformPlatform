using Account.Features.FeatureFlags.Domain;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.ExecutionContext;
using SharedKernel.FeatureFlags;

namespace Account.Features.FeatureFlags.Queries;

[PublicAPI]
public sealed record GetTenantConfigurableFeatureFlagsQuery : IRequest<Result<TenantConfigurableFeatureFlagsResponse>>;

[PublicAPI]
public sealed record TenantConfigurableFeatureFlagsResponse(TenantConfigurableFeatureFlag[] Flags);

[PublicAPI]
public sealed record TenantConfigurableFeatureFlag(string FlagKey, bool Enabled);

public sealed class GetTenantConfigurableFeatureFlagsHandler(IFeatureFlagRepository featureFlagRepository, IExecutionContext executionContext)
    : IRequestHandler<GetTenantConfigurableFeatureFlagsQuery, Result<TenantConfigurableFeatureFlagsResponse>>
{
    public async Task<Result<TenantConfigurableFeatureFlagsResponse>> Handle(GetTenantConfigurableFeatureFlagsQuery query, CancellationToken cancellationToken)
    {
        var tenantId = executionContext.TenantId!;

        var configurableDefinitions = SharedKernel.FeatureFlags.FeatureFlags.GetAll()
            .Where(f => f is { Scope: FeatureFlagScope.Tenant, ConfigurableByTenant: true })
            .ToArray();

        var allRows = await featureFlagRepository.GetTenantScopedRowsAsync(tenantId, cancellationToken);
        var baseRows = allRows.Where(r => r.TenantId is null && r.UserId is null).ToDictionary(r => r.FlagKey);
        var tenantOverrides = allRows.Where(r => r.TenantId == tenantId && r.UserId is null).ToDictionary(r => r.FlagKey);

        // Hide flags an admin has globally deactivated via the BackOffice kill switch. The user-facing
        // toggle would otherwise let a tenant flip a row whose evaluation is short-circuited at the base.
        var flags = configurableDefinitions
            .Where(definition => baseRows.TryGetValue(definition.Key, out var baseRow) && baseRow.IsActive)
            .Select(definition =>
                {
                    tenantOverrides.TryGetValue(definition.Key, out var tenantOverride);
                    var enabled = tenantOverride?.IsActive == true;
                    return new TenantConfigurableFeatureFlag(definition.Key, enabled);
                }
            ).ToArray();

        return new TenantConfigurableFeatureFlagsResponse(flags);
    }
}
