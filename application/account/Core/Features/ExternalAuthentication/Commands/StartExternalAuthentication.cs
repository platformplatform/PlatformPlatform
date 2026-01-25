using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using PlatformPlatform.Account.Features.ExternalAuthentication.Domain;
using PlatformPlatform.Account.Integrations.OAuth;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.OpenIdConnect;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.Account.Features.ExternalAuthentication.Commands;

[PublicAPI]
public sealed record StartExternalLoginCommand(TenantId? PreferredTenantId = null) : ICommand, IRequest<Result<string>>
{
    [JsonIgnore]
    public ExternalProviderType ProviderType { get; init; }
}

public sealed class StartExternalLoginHandler(
    IExternalLoginRepository externalLoginRepository,
    OAuthProviderFactory oauthProviderFactory,
    ExternalAuthenticationService externalAuthenticationService,
    IHttpContextAccessor httpContextAccessor,
    ITelemetryEventsCollector events
) : IRequestHandler<StartExternalLoginCommand, Result<string>>
{
    public async Task<Result<string>> Handle(StartExternalLoginCommand command, CancellationToken cancellationToken)
    {
        return await StartExternalAuthenticationHelper.StartFlow(
            command.ProviderType, ExternalLoginType.Login, command.PreferredTenantId,
            externalLoginRepository, oauthProviderFactory, externalAuthenticationService, httpContextAccessor, events, cancellationToken
        );
    }
}

[PublicAPI]
public sealed record StartExternalSignupCommand : ICommand, IRequest<Result<string>>
{
    [JsonIgnore]
    public ExternalProviderType ProviderType { get; init; }
}

public sealed class StartExternalSignupHandler(
    IExternalLoginRepository externalLoginRepository,
    OAuthProviderFactory oauthProviderFactory,
    ExternalAuthenticationService externalAuthenticationService,
    IHttpContextAccessor httpContextAccessor,
    ITelemetryEventsCollector events
) : IRequestHandler<StartExternalSignupCommand, Result<string>>
{
    public async Task<Result<string>> Handle(StartExternalSignupCommand command, CancellationToken cancellationToken)
    {
        return await StartExternalAuthenticationHelper.StartFlow(
            command.ProviderType, ExternalLoginType.Signup, null,
            externalLoginRepository, oauthProviderFactory, externalAuthenticationService, httpContextAccessor, events, cancellationToken
        );
    }
}

internal static class StartExternalAuthenticationHelper
{
    public static async Task<Result<string>> StartFlow(
        ExternalProviderType providerType,
        ExternalLoginType loginType,
        TenantId? preferredTenantId,
        IExternalLoginRepository externalLoginRepository,
        OAuthProviderFactory oauthProviderFactory,
        ExternalAuthenticationService externalAuthenticationService,
        IHttpContextAccessor httpContextAccessor,
        ITelemetryEventsCollector events,
        CancellationToken cancellationToken
    )
    {
        var httpContext = httpContextAccessor.HttpContext!;
        var useMockProvider = oauthProviderFactory.ShouldUseMockProvider(httpContext);

        var oauthProvider = oauthProviderFactory.GetProvider(providerType, useMockProvider);
        if (oauthProvider is null)
        {
            return Result<string>.BadRequest($"Provider '{providerType}' is not configured.");
        }

        var codeVerifier = PkceUtilities.GenerateCodeVerifier();
        var codeChallenge = PkceUtilities.GenerateCodeChallenge(codeVerifier);
        var nonce = NonceUtilities.GenerateNonce();

        var browserFingerprint = externalAuthenticationService.GenerateBrowserFingerprintHash();

        var externalLogin = ExternalLogin.Create(loginType, providerType, codeVerifier, nonce, browserFingerprint);
        await externalLoginRepository.AddAsync(externalLogin, cancellationToken);

        var stateToken = externalAuthenticationService.ProtectState(externalLogin.Id);
        externalAuthenticationService.SetExternalLoginCookie(externalLogin.Id, preferredTenantId);

        var returnPath = httpContext.Request.Query["ReturnPath"].ToString();
        if (!string.IsNullOrEmpty(returnPath))
        {
            ReturnPathHelper.SetReturnPathCookie(httpContext, returnPath);
        }

        var locale = httpContext.Request.Query["Locale"].ToString();
        if (!string.IsNullOrEmpty(locale))
        {
            externalAuthenticationService.SetLocaleCookie(locale);
        }

        var redirectUri = ExternalAuthenticationService.GetRedirectUri(providerType, loginType);
        var authorizationUrl = oauthProvider.BuildAuthorizationUrl(stateToken, codeChallenge, nonce, redirectUri);

        TelemetryEvent telemetryEvent = loginType == ExternalLoginType.Login
            ? new ExternalLoginStarted(providerType)
            : new ExternalSignupStarted(providerType);
        events.CollectEvent(telemetryEvent);

        return Result<string>.Redirect(authorizationUrl);
    }
}
