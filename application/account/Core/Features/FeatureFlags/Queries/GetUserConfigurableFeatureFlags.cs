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
        var tenantId = executionContext.TenantId!.Value;
        var userId = executionContext.UserInfo.Id!;

        var configurableDefinitions = SharedKernel.FeatureFlags.FeatureFlags.GetAll()
            .Where(f => f is { Scope: FeatureFlagScope.User, ConfigurableByUser: true })
            .ToArray();

        var allRows = await featureFlagRepository.GetAllRelevantRowsAsync(tenantId, userId, cancellationToken);
        var userOverrides = allRows.Where(r => r.TenantId == tenantId && r.UserId == userId).ToDictionary(r => r.FlagKey);

        var flags = configurableDefinitions
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
