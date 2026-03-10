using Account.Features.FeatureFlags;
using Account.Features.Subscriptions.Domain;
using Account.Features.Tenants.Domain;
using Account.Features.Users.Domain;
using SharedKernel.Authentication;
using SharedKernel.Authentication.TokenGeneration;
using SharedKernel.Cqrs;

namespace Account.Features.Users.Shared;

/// <summary>
///     Factory for creating UserInfo instances with tenant information.
///     Centralizes the logic for creating UserInfo to follow SRP and avoid duplication.
/// </summary>
public sealed class UserInfoFactory(ITenantRepository tenantRepository, ISubscriptionRepository subscriptionRepository, FeatureFlagEvaluationService featureFlagEvaluationService)
{
    /// <summary>
    ///     Creates a UserInfo instance from a User entity, including tenant name.
    ///     Returns a failure result if the tenant has been soft-deleted.
    /// </summary>
    public async Task<Result<UserInfo>> CreateUserInfoAsync(User user, SessionId? sessionId, CancellationToken cancellationToken)
    {
        var tenant = await tenantRepository.GetByIdAsync(user.TenantId, cancellationToken);
        if (tenant is null) return Result<UserInfo>.BadRequest("Tenant has been deleted.");

        var subscription = await subscriptionRepository.GetByTenantIdUnfilteredAsync(user.TenantId, cancellationToken);

        var enabledFlags = await featureFlagEvaluationService.EvaluateAsync(tenant.Id.Value, user.Id.Value, tenant.RolloutBucket, user.RolloutBucket, cancellationToken);

        return new UserInfo
        {
            IsAuthenticated = true,
            Id = user.Id,
            TenantId = user.TenantId,
            SessionId = sessionId,
            Role = user.Role.ToString(),
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Title = user.Title,
            AvatarUrl = user.Avatar.Url,
            TenantName = tenant.Name,
            TenantLogoUrl = tenant.Logo.Url,
            SubscriptionPlan = subscription!.Plan.ToString(),
            Locale = user.Locale,
            IsInternalUser = user.IsInternalUser,
            FeatureFlags = new HashSet<string>(enabledFlags),
            TenantRolloutBucket = tenant.RolloutBucket,
            UserRolloutBucket = user.RolloutBucket
        };
    }
}
