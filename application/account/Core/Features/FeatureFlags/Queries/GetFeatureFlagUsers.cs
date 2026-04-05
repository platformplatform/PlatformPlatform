using Account.Features.FeatureFlags.Domain;
using Account.Features.Tenants.Domain;
using Account.Features.Users.Domain;
using FluentValidation;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Domain;

namespace Account.Features.FeatureFlags.Queries;

[PublicAPI]
public sealed record GetFeatureFlagUsersQuery : IRequest<Result<GetFeatureFlagUsersResponse>>
{
    [JsonIgnore] // Removes from API contract
    public FeatureFlagKey FeatureFlagKey { get; init; } = null!;

    public string? Search { get; init; }
}

[PublicAPI]
public sealed record GetFeatureFlagUsersResponse(FeatureFlagUserInfo[] Users);

[PublicAPI]
public sealed record FeatureFlagUserInfo(
    UserId UserId,
    TenantId TenantId,
    string Email,
    string TenantName,
    int RolloutBucket,
    bool IsEnabled,
    FeatureFlagOverrideSource Source
);

public sealed class GetFeatureFlagUsersValidator : AbstractValidator<GetFeatureFlagUsersQuery>
{
    public GetFeatureFlagUsersValidator()
    {
        RuleFor(x => x.FeatureFlagKey)
            .NotEmpty().WithMessage("Feature flag key must not be empty.")
            .Must(key => SharedKernel.Domain.FeatureFlags.Get(key) is not null).WithMessage("Feature flag key must exist in the registry.")
            .Must(key => SharedKernel.Domain.FeatureFlags.Get(key)?.Scope == FeatureFlagScope.User).WithMessage("Feature flag must have user scope.");

        RuleFor(x => x.Search).MaximumLength(100).WithMessage("The search term must be at most 100 characters.");
    }
}

public sealed class GetFeatureFlagUsersHandler(IFeatureFlagRepository featureFlagRepository, IUserRepository userRepository, ITenantRepository tenantRepository)
    : IRequestHandler<GetFeatureFlagUsersQuery, Result<GetFeatureFlagUsersResponse>>
{
    public async Task<Result<GetFeatureFlagUsersResponse>> Handle(GetFeatureFlagUsersQuery query, CancellationToken cancellationToken)
    {
        var featureFlagDefinition = SharedKernel.Domain.FeatureFlags.Get(query.FeatureFlagKey);
        if (featureFlagDefinition is null) return Result<GetFeatureFlagUsersResponse>.NotFound($"Feature flag with key '{query.FeatureFlagKey}' not found.");

        if (string.IsNullOrWhiteSpace(query.Search)) return new GetFeatureFlagUsersResponse([]);

        var users = await userRepository.SearchByEmailUnfilteredAsync(query.Search.Trim(), cancellationToken);
        var userOverrides = await featureFlagRepository.GetUserOverridesForFlagAsync(query.FeatureFlagKey, cancellationToken);
        var featureFlagsByUserId = userOverrides.ToDictionary(f => f.UserId!);

        var baseFeatureFlag = await featureFlagRepository.GetBaseFeatureFlagByKeyAsync(query.FeatureFlagKey, cancellationToken);

        var tenantIds = users.Select(u => u.TenantId).Distinct().ToArray();
        var tenants = await tenantRepository.GetByIdsUnfilteredAsync(tenantIds, cancellationToken);
        var tenantsById = tenants.ToDictionary(t => t.Id);

        var featureFlagUsers = users.Select(user =>
            {
                var tenantName = tenantsById.TryGetValue(user.TenantId, out var tenant) ? tenant.Name : "Unknown";

                if (featureFlagsByUserId.TryGetValue(user.Id, out var userFeatureFlag))
                {
                    var isEnabled = userFeatureFlag.EnabledAt is not null && (userFeatureFlag.DisabledAt is null || userFeatureFlag.EnabledAt > userFeatureFlag.DisabledAt);
                    return new FeatureFlagUserInfo(user.Id, user.TenantId, user.Email, tenantName, user.RolloutBucket, isEnabled, FeatureFlagOverrideSource.ManualOverride);
                }

                if (featureFlagDefinition.IsAbTestEligible && baseFeatureFlag?.RolloutBucketStart is not null && baseFeatureFlag.RolloutBucketEnd is not null)
                {
                    var isInRange = RolloutBucketHasher.IsInRolloutBucketRange(user.RolloutBucket, baseFeatureFlag.RolloutBucketStart.Value, baseFeatureFlag.RolloutBucketEnd.Value);
                    return new FeatureFlagUserInfo(user.Id, user.TenantId, user.Email, tenantName, user.RolloutBucket, isInRange, FeatureFlagOverrideSource.AbRollout);
                }

                return new FeatureFlagUserInfo(user.Id, user.TenantId, user.Email, tenantName, user.RolloutBucket, false, FeatureFlagOverrideSource.Default);
            }
        ).ToArray();

        return new GetFeatureFlagUsersResponse(featureFlagUsers);
    }
}
