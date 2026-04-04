using Account.Features.FeatureFlags.Domain;
using Account.Features.Tenants.Domain;
using Account.Features.Users.Domain;
using FluentValidation;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Domain;
using SharedKernel.FeatureFlags;

namespace Account.Features.FeatureFlags.Queries;

[PublicAPI]
public sealed record GetFlagUsersQuery : IRequest<Result<GetFlagUsersResponse>>
{
    [JsonIgnore] // Removes from API contract
    public string FlagKey { get; init; } = null!;
}

[PublicAPI]
public sealed record GetFlagUsersResponse(FlagUserInfo[] Users);

[PublicAPI]
public sealed record FlagUserInfo(
    UserId UserId,
    TenantId TenantId,
    string Email,
    string TenantName,
    int RolloutBucket,
    bool IsEnabled,
    string Source
);

public sealed class GetFlagUsersValidator : AbstractValidator<GetFlagUsersQuery>
{
    public GetFlagUsersValidator()
    {
        RuleFor(x => x.FlagKey)
            .NotEmpty().WithMessage("Flag key must not be empty.")
            .Must(key => SharedKernel.FeatureFlags.FeatureFlags.Get(key) is not null).WithMessage("Flag key must exist in the registry.")
            .Must(key => SharedKernel.FeatureFlags.FeatureFlags.Get(key)?.Scope == FeatureFlagScope.User).WithMessage("Flag must have user scope.");
    }
}

public sealed class GetFlagUsersHandler(IFeatureFlagRepository featureFlagRepository, IUserRepository userRepository, ITenantRepository tenantRepository)
    : IRequestHandler<GetFlagUsersQuery, Result<GetFlagUsersResponse>>
{
    public async Task<Result<GetFlagUsersResponse>> Handle(GetFlagUsersQuery query, CancellationToken cancellationToken)
    {
        var definition = SharedKernel.FeatureFlags.FeatureFlags.Get(query.FlagKey);
        if (definition is null) return Result<GetFlagUsersResponse>.NotFound($"Feature flag with key '{query.FlagKey}' not found.");

        var userOverrides = await featureFlagRepository.GetUserOverridesForFlagAsync(query.FlagKey, cancellationToken);
        if (userOverrides.Length == 0) return new GetFlagUsersResponse([]);

        var userIds = userOverrides.Select(f => new UserId(f.UserId!)).ToArray();
        var users = await userRepository.GetByIdsUnfilteredAsync(userIds, cancellationToken);
        var usersById = users.ToDictionary(u => u.Id.Value);

        var tenantIds = users.Select(u => u.TenantId).Distinct().ToArray();
        var tenants = await tenantRepository.GetByIdsUnfilteredAsync(tenantIds, cancellationToken);
        var tenantsById = tenants.ToDictionary(t => t.Id);

        var flagUsers = userOverrides.Select(overrideRow =>
            {
                var userId = new UserId(overrideRow.UserId!);
                var user = usersById.GetValueOrDefault(userId.Value);
                var tenantName = user is not null && tenantsById.TryGetValue(user.TenantId, out var tenant) ? tenant.Name : "Unknown";
                var isEnabled = overrideRow.EnabledAt is not null && (overrideRow.DisabledAt is null || overrideRow.EnabledAt > overrideRow.DisabledAt);
                var tenantId = user?.TenantId ?? new TenantId(0);
                var rolloutBucket = user?.RolloutBucket ?? 0;
                return new FlagUserInfo(userId, tenantId, user?.Email ?? "Unknown", tenantName, rolloutBucket, isEnabled, "manual_override");
            }
        ).ToArray();

        return new GetFlagUsersResponse(flagUsers);
    }
}
