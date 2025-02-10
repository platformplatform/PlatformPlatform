using System.Security.Claims;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.SinglePageApp;

namespace PlatformPlatform.SharedKernel.Authentication;

/// <summary>
///     Provides details about the authenticated user making the current request, including user identity, role,
///     contact information, and additional profile details extracted from claims.
/// </summary>
public class UserInfo
{
    private const string DefaultLocale = "en-US";

    /// <summary>
    ///     Represents the system user, typically used for background tasks or where no user is directly authenticated.
    /// </summary>
    public static readonly UserInfo System = new()
    {
        IsAuthenticated = false,
        Locale = DefaultLocale
    };

    public bool IsAuthenticated { get; init; }

    public string? Locale { get; init; }

    public UserId? Id { get; init; }

    public TenantId? TenantId { get; init; }

    public string? Role { get; init; }

    public string? Email { get; init; }

    public string? FirstName { get; init; }

    public string? LastName { get; init; }

    public string? Title { get; init; }

    public string? AvatarUrl { get; init; }

    public static UserInfo Create(ClaimsPrincipal? user, string? browserLocale)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            return new UserInfo
            {
                IsAuthenticated = user?.Identity?.IsAuthenticated ?? false,
                Locale = GetValidLocale(browserLocale)
            };
        }

        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        var tenantId = user.FindFirstValue("tenant_id");
        return new UserInfo
        {
            IsAuthenticated = true,
            Id = userId == null ? null : new UserId(userId),
            TenantId = tenantId == null ? null : new TenantId(long.Parse(tenantId)),
            Role = user.FindFirstValue(ClaimTypes.Role),
            Email = user.FindFirstValue(ClaimTypes.Email),
            FirstName = user.FindFirstValue(ClaimTypes.GivenName),
            LastName = user.FindFirstValue(ClaimTypes.Surname),
            Title = user.FindFirstValue("title"),
            AvatarUrl = user.FindFirstValue("avatar_url"),
            Locale = GetValidLocale(user.FindFirstValue("locale"))
        };
    }

    private static string GetValidLocale(string? locale)
    {
        if (string.IsNullOrEmpty(locale))
        {
            return DefaultLocale;
        }

        if (SinglePageAppConfiguration.SupportedLocalizations.Contains(locale, StringComparer.OrdinalIgnoreCase))
        {
            return locale;
        }

        // Fallback to base language. E.g. if locale is `en-UK` use `en` which would then return `en-US`
        var baseLanguageCode = locale[..2];
        var foundLocale = SinglePageAppConfiguration.SupportedLocalizations
            .FirstOrDefault(sl => sl.StartsWith(baseLanguageCode, StringComparison.OrdinalIgnoreCase));

        return foundLocale ?? DefaultLocale;
    }
}
