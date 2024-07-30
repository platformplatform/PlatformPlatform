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
            Id = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Email = user.FindFirst(ClaimTypes.Email)?.Value;
            FirstName = user.FindFirst(ClaimTypes.GivenName)?.Value;
            LastName = user.FindFirst(ClaimTypes.Surname)?.Value;
            Role = user.FindFirst(ClaimTypes.Role)?.Value;
            TenantId = user.FindFirst("tenantId")?.Value;
            AvatarUrl = user.FindFirst("picture")?.Value;
        }
    }

    public string? Id { get; init; }

    public bool IsAuthenticated { get; init; }

    public string Locale { get; init; }

    public string? Email { get; init; }

    public string? FirstName { get; init; }

    public string? LastName { get; init; }

    public string? Role { get; init; }

    public string? TenantId { get; init; }

    public string? AvatarUrl { get; init; }
}
