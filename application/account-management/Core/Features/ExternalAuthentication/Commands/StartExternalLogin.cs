using System.Security.Cryptography;
using System.Text;
using JetBrains.Annotations;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using PlatformPlatform.AccountManagement.Features.ExternalAuthentication.Domain;
using PlatformPlatform.AccountManagement.Integrations.OAuth;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.OpenIdConnect;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.AccountManagement.Features.ExternalAuthentication.Commands;

[PublicAPI]
public sealed record StartExternalLoginCommand(string? ReturnPath, string? Locale) : ICommand, IRequest<Result<StartExternalLoginResponse>>
{
    [JsonIgnore]
    public ExternalProviderType ProviderType { get; init; }
}

[PublicAPI]
public sealed record StartExternalLoginResponse(string AuthorizationUrl);

public sealed class StartExternalLoginHandler(
    IExternalLoginRepository externalLoginRepository,
    OAuthProviderFactory oauthProviderFactory,
    IHttpContextAccessor httpContextAccessor,
    IDataProtectionProvider dataProtectionProvider,
    ITelemetryEventsCollector events
) : IRequestHandler<StartExternalLoginCommand, Result<StartExternalLoginResponse>>
{
    private const string DataProtectionPurpose = "ExternalLogin";
    private const string ExternalLoginCookieName = "__Host_External_Login";

    public async Task<Result<StartExternalLoginResponse>> Handle(StartExternalLoginCommand command, CancellationToken cancellationToken)
    {
        var httpContext = httpContextAccessor.HttpContext!;

        var useMockProvider = oauthProviderFactory.ShouldUseMockProvider(httpContext);

        var oauthProvider = oauthProviderFactory.GetProvider(command.ProviderType, useMockProvider);
        if (oauthProvider is null)
        {
            return Result<StartExternalLoginResponse>.BadRequest($"Provider '{command.ProviderType}' is not configured.");
        }

        var codeVerifier = PkceUtilities.GenerateCodeVerifier();
        var codeChallenge = PkceUtilities.GenerateCodeChallenge(codeVerifier);

        var userAgent = httpContext.Request.Headers.UserAgent.ToString();
        var acceptLanguage = httpContext.Request.Headers.AcceptLanguage.ToString();
        var browserFingerprint = ComputeBrowserFingerprint(userAgent, acceptLanguage);

        var externalLogin = ExternalLogin.Create(
            command.ProviderType,
            ExternalFlowType.Login,
            codeVerifier,
            browserFingerprint,
            command.ReturnPath,
            command.Locale
        );

        var dataProtector = dataProtectionProvider.CreateProtector(DataProtectionPurpose);
        var stateToken = dataProtector.Protect(externalLogin.Id.ToString());
        externalLogin.SetStateToken(stateToken);

        await externalLoginRepository.AddAsync(externalLogin, cancellationToken);

        SetExternalLoginCookie(httpContext, externalLogin.Id, browserFingerprint);

        if (!string.IsNullOrEmpty(command.ReturnPath))
        {
            ReturnPathHelper.SetReturnPathCookie(httpContext, command.ReturnPath);
        }

        var redirectUri = GetRedirectUri(httpContext, command.ProviderType);
        var authorizationUrl = oauthProvider.BuildAuthorizationUrl(stateToken, codeChallenge, redirectUri);

        events.CollectEvent(new ExternalLoginStarted(command.ProviderType));

        return new StartExternalLoginResponse(authorizationUrl);
    }

    private static string ComputeBrowserFingerprint(string userAgent, string acceptLanguage)
    {
        var fingerprintSource = $"{userAgent}|{acceptLanguage}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(fingerprintSource));
        return Convert.ToBase64String(hash);
    }

    private static void SetExternalLoginCookie(HttpContext httpContext, ExternalLoginId externalLoginId, string fingerprintHash)
    {
        var cookieValue = $"{externalLoginId}|{fingerprintHash}";
        httpContext.Response.Cookies.Append(
            ExternalLoginCookieName,
            cookieValue,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
                MaxAge = TimeSpan.FromSeconds(ExternalLogin.ValidForSeconds)
            }
        );
    }

    private static string GetRedirectUri(HttpContext httpContext, ExternalProviderType providerType)
    {
        var scheme = httpContext.Request.Scheme;
        var host = httpContext.Request.Host;
        return $"{scheme}://{host}/api/account-management/authentication/{providerType}/login/callback";
    }
}
