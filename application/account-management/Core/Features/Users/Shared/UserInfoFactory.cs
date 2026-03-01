using PlatformPlatform.AccountManagement.Features.Tenants.Domain;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Authentication;
using PlatformPlatform.SharedKernel.Authentication.TokenGeneration;
using PlatformPlatform.SharedKernel.Cqrs;

namespace PlatformPlatform.AccountManagement.Features.Users.Shared;

/// <summary>
///     Factory for creating UserInfo instances with tenant information.
///     Centralizes the logic for creating UserInfo to follow SRP and avoid duplication.
/// </summary>
public sealed class UserInfoFactory(ITenantRepository tenantRepository)
{
    /// <summary>
    ///     Creates a UserInfo instance from a User entity, including tenant name.
    ///     Returns a failure result if the tenant has been soft-deleted.
    /// </summary>
    public async Task<Result<UserInfo>> CreateUserInfoAsync(User user, SessionId? sessionId, CancellationToken cancellationToken)
    {
        var tenant = await tenantRepository.GetByIdAsync(user.TenantId, cancellationToken);
        if (tenant is null) return Result<UserInfo>.BadRequest("Tenant has been deleted.");

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
            Locale = user.Locale,
            IsInternalUser = user.IsInternalUser
        };
    }
}
