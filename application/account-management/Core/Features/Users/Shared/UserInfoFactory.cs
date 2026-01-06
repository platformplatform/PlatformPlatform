using PlatformPlatform.AccountManagement.Features.Tenants.Domain;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Authentication;
using PlatformPlatform.SharedKernel.Authentication.TokenGeneration;

namespace PlatformPlatform.AccountManagement.Features.Users.Shared;

/// <summary>
///     Factory for creating UserInfo instances with tenant information.
///     Centralizes the logic for creating UserInfo to follow SRP and avoid duplication.
/// </summary>
public sealed class UserInfoFactory(ITenantRepository tenantRepository)
{
    /// <summary>
    ///     Creates a UserInfo instance from a User entity, including tenant name.
    /// </summary>
    /// <param name="user">The user entity</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="sessionId">Optional session ID to include in the UserInfo</param>
    /// <returns>UserInfo with all required properties including tenant name</returns>
    public async Task<UserInfo> CreateUserInfoAsync(User user, CancellationToken cancellationToken, SessionId? sessionId = null)
    {
        var tenant = await tenantRepository.GetByIdAsync(user.TenantId, cancellationToken);

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
            TenantName = tenant?.Name,
            TenantLogoUrl = tenant?.Logo.Url,
            Locale = user.Locale,
            IsInternalUser = user.IsInternalUser
        };
    }
}
