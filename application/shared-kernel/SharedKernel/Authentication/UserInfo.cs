using System.Security.Claims;

namespace PlatformPlatform.SharedKernel.Authentication;

public class UserInfo
{
    private UserInfo()
    {
    }

    public bool IsAuthenticated { get; init; }

    public string? Locale { get; init; }

    public string? UserId { get; private set; }

    public string? TenantId { get; private set; }

    public string? UserRole { get; private set; }

    public string? Email { get; private set; }

    public string? FirstName { get; private set; }

    public string? LastName { get; private set; }

    public string? Title { get; private set; }

    public string? AvatarUrl { get; private set; }

    public static UserInfo Create(ClaimsPrincipal user, string defaultLocale)
    {
        var userInfo = new UserInfo
        {
            IsAuthenticated = user.Identity?.IsAuthenticated ?? false,
            Locale = defaultLocale
        };

        if (userInfo.IsAuthenticated)
        {
            userInfo.UserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            userInfo.TenantId = user.FindFirst("tenant_id")?.Value;
            userInfo.UserRole = user.FindFirst(ClaimTypes.Role)?.Value;
            userInfo.Email = user.FindFirst(ClaimTypes.Email)?.Value;
            userInfo.FirstName = user.FindFirst(ClaimTypes.GivenName)?.Value;
            userInfo.LastName = user.FindFirst(ClaimTypes.Surname)?.Value;
            userInfo.Title = user.FindFirst("title")?.Value;
            userInfo.AvatarUrl = user.FindFirst("avatar_url")?.Value;
        }

        return userInfo;
    }
}
