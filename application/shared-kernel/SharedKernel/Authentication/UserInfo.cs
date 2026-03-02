using System.Security.Claims;
using PlatformPlatform.SharedKernel.Authentication.TokenGeneration;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.Platform;
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
        Locale = DefaultLocale,
        IsInternalUser = false
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

    public string? TenantName { get; init; }

    public string? TenantLogoUrl { get; init; }

    public string? SubscriptionPlan { get; init; }

    public bool IsInternalUser { get; init; }

    public string? ZoomLevel { get; init; }

    public string? Theme { get; init; }

    public SessionId? SessionId { get; init; }

    public static UserInfo Create(ClaimsPrincipal? user, string? browserLocale, string? zoomLevel = null, string? theme = null)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            return new UserInfo
            {
                IsAuthenticated = user?.Identity?.IsAuthenticated ?? false,
                Locale = GetValidLocale(browserLocale),
                ZoomLevel = zoomLevel,
                Theme = theme,
                IsInternalUser = false
            };
        }

        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        var tenantId = user.FindFirstValue("tenant_id");
        var sessionId = user.FindFirstValue("session_id");
        var email = user.FindFirstValue(ClaimTypes.Email);
        return new UserInfo
        {
            IsAuthenticated = true,
            Id = userId == null ? null : new UserId(userId),
            TenantId = tenantId == null ? null : new TenantId(long.Parse(tenantId)),
            SessionId = sessionId == null ? null : new SessionId(sessionId),
            Role = user.FindFirstValue(ClaimTypes.Role),
            Email = email,
            FirstName = user.FindFirstValue(ClaimTypes.GivenName),
            LastName = user.FindFirstValue(ClaimTypes.Surname),
            Title = user.FindFirstValue("title"),
            AvatarUrl = user.FindFirstValue("avatar_url"),
            TenantName = user.FindFirstValue("tenant_name"),
            TenantLogoUrl = user.FindFirstValue("tenant_logo_url"),
            SubscriptionPlan = user.FindFirstValue("subscription_plan"),
            Locale = GetValidLocale(user.FindFirstValue("locale")),
            ZoomLevel = zoomLevel,
            Theme = theme,
            IsInternalUser = IsInternalUserEmail(email)
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

    private static bool IsInternalUserEmail(string? email)
    {
        return email is not null && email.EndsWith(Settings.Current.Identity.InternalEmailDomain, StringComparison.OrdinalIgnoreCase);
    }
}
