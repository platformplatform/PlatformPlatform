using System.Security.Claims;

namespace PlatformPlatform.SharedKernel.ApiCore.SinglePageApp;

public class UserInfo
{
    public UserInfo(ClaimsPrincipal user, string defaultLocale)
    {
        IsAuthenticated = user.Identity?.IsAuthenticated ?? false;
        Locale = user.FindFirst("locale")?.Value ?? defaultLocale;

        if (IsAuthenticated)
        {
            UserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Email = user.FindFirst(ClaimTypes.Email)?.Value;
            TenantId = user.FindFirst("tenant_id")?.Value;
            UserRole = user.FindFirst(ClaimTypes.Role)?.Value;
            FirstName = user.FindFirst(ClaimTypes.GivenName)?.Value;
            LastName = user.FindFirst(ClaimTypes.Surname)?.Value;
            Title = user.FindFirst("title")?.Value;
            AvatarUrl = user.FindFirst("avatar_url")?.Value;
        }
    }

    public bool IsAuthenticated { get; init; }

    public string Locale { get; init; }

    public string? UserId { get; init; }

    public string? TenantId { get; init; }

    public string? UserRole { get; init; }

    public string? Email { get; init; }

    public string? FirstName { get; init; }

    public string? LastName { get; init; }

    public string? Title { get; init; }

    public string? AvatarUrl { get; init; }
}
