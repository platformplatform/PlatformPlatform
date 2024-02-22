using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace PlatformPlatform.AccountManagement.Api.Auth.JwtCookieAuthentication;

internal sealed class JwtCookieConfigureOptions(IDataProtectionProvider dataProtectionProvider)
    : IConfigureNamedOptions<JwtCookieAuthenticationOptions>
{
    private const string PrimaryPurpose = "PlatformPlatform.AccountManagement.Api.Auth.JwtCookieAuthentication";

    public void Configure(string? name, JwtCookieAuthenticationOptions options)
    {
        if (name is null) return;

        options.RefreshTokenProtector =
            new TicketDataFormat(dataProtectionProvider.CreateProtector(PrimaryPurpose, name, "RefreshToken"));
    }

    public void Configure(JwtCookieAuthenticationOptions options)
    {
        throw new NotImplementedException();
    }
}