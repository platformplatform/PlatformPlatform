using Account.Features.FeatureFlags.Domain;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.ExecutionContext;
using SharedKernel.FeatureFlags;

namespace Account.Features.FeatureFlags.Queries;

[PublicAPI]
public sealed record GetUserConfigurableFeatureFlagsQuery : IRequest<Result<UserConfigurableFeatureFlagsResponse>>;

[PublicAPI]
public sealed record UserConfigurableFeatureFlagsResponse(UserConfigurableFeatureFlag[] Flags);

[PublicAPI]
public sealed record UserConfigurableFeatureFlag(string FlagKey, bool Enabled);

public sealed class GetUserConfigurableFeatureFlagsHandler(IFeatureFlagRepository featureFlagRepository, IExecutionContext executionContext)
    : IRequestHandler<GetUserConfigurableFeatureFlagsQuery, Result<UserConfigurableFeatureFlagsResponse>>
{
    public async Task<Result<UserConfigurableFeatureFlagsResponse>> Handle(GetUserConfigurableFeatureFlagsQuery query, CancellationToken cancellationToken)
    {
        var tenantId = executionContext.TenantId!;
        var userId = executionContext.UserInfo.Id!;

        var configurableDefinitions = SharedKernel.FeatureFlags.FeatureFlags.GetAll()
            .Where(f => f is { Scope: FeatureFlagScope.User, ConfigurableByUser: true })
            .ToArray();

        var allRows = await featureFlagRepository.GetUserScopedRowsAsync(tenantId, userId, cancellationToken);
        var baseRows = allRows.Where(r => r.TenantId is null && r.UserId is null).ToDictionary(r => r.FlagKey);
        var userOverrides = allRows.Where(r => r.TenantId == tenantId && r.UserId == userId).ToDictionary(r => r.FlagKey);

        // Hide flags an admin has globally deactivated via the BackOffice kill switch. The user-facing
        // toggle would otherwise let a user flip a row whose evaluation is short-circuited at the base.
        var flags = configurableDefinitions
            .Where(definition => baseRows.TryGetValue(definition.Key, out var baseRow) && baseRow.IsActive)
            .Select(definition =>
                {
                    userOverrides.TryGetValue(definition.Key, out var userOverride);
                    var enabled = userOverride?.IsActive == true;
                    return new UserConfigurableFeatureFlag(definition.Key, enabled);
                }
            ).ToArray();

        return new UserConfigurableFeatureFlagsResponse(flags);
    }
}
