using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Tokens;

namespace PlatformPlatform.AccountManagement.Api.Auth.JwtCookieAuthentication;

public class JwtCookieAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "JwtCookieAuthenticationScheme";

    private ISecureDataFormat<AuthenticationTicket>? _refreshTokenProtector;

    public string AccessTokenName { get; set; } = "X-Access-Token";

    public string RefreshTokenName { get; set; } = "X-Refresh-Token";

    public TimeSpan NotBeforeTimeSpan { get; set; } = TimeSpan.FromSeconds(-30);

    public TimeSpan RefreshTokenExpireTimeSpan { get; set; } = TimeSpan.FromDays(30);

    public TimeSpan AccessTokenExpireTimeSpan { get; set; } = TimeSpan.FromMinutes(10);

    public TokenValidationParameters TokenValidationParameters { get; set; } = new();

    public SymmetricSecurityKey? SigningSecurityKey { get; set; }

    public ISecureDataFormat<AuthenticationTicket> RefreshTokenProtector
    {
        get => _refreshTokenProtector ??
               throw new InvalidOperationException($"{nameof(RefreshTokenProtector)} was not set.");
        set => _refreshTokenProtector = value;
    }
}