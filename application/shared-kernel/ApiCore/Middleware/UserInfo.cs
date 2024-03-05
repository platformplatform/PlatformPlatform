using System.Security.Claims;

namespace PlatformPlatform.SharedKernel.ApiCore.Middleware;

[UsedImplicitly(ImplicitUseTargetFlags.Members)]
public class UserInfo
{
    public UserInfo(ClaimsPrincipal user, string defaultLocale)
    {
        IsAuthenticated = user.Identity?.IsAuthenticated ?? false;
        Locale = user.FindFirst("locale")?.Value ?? defaultLocale;

        if (IsAuthenticated)
        {
            Email = user.Identity?.Name;
            Name = user.FindFirst(ClaimTypes.Name)?.Value;
            Role = user.FindFirst(ClaimTypes.Role)?.Value;
            TenantId = user.FindFirst("tenantId")?.Value;
        }
    }

    public bool IsAuthenticated { get; init; }

    public string Locale { get; init; }

    public string? Email { get; init; }

    public string? Name { get; init; }

    public string? Role { get; init; }

    public string? TenantId { get; init; }
}