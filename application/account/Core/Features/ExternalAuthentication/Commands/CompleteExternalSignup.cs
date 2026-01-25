using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using PlatformPlatform.Account.Features.Authentication.Domain;
using PlatformPlatform.Account.Features.ExternalAuthentication.Domain;
using PlatformPlatform.Account.Features.ExternalAuthentication.Shared;
using PlatformPlatform.Account.Features.Tenants.Commands;
using PlatformPlatform.Account.Features.Users.Domain;
using PlatformPlatform.Account.Features.Users.Shared;
using PlatformPlatform.Account.Integrations.OAuth;
using PlatformPlatform.SharedKernel.Authentication.TokenGeneration;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.OpenIdConnect;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.Account.Features.ExternalAuthentication.Commands;

[PublicAPI]
public sealed record CompleteExternalSignupCommand(string? Code, string? State, string? Error, string? ErrorDescription)
    : ICommand, IRequest<Result<string>>
{
    [JsonIgnore]
    public string? Provider { get; init; }
}

public sealed class CompleteExternalSignupHandler(
    IExternalLoginRepository externalLoginRepository,
    IUserRepository userRepository,
    ISessionRepository sessionRepository,
    UserInfoFactory userInfoFactory,
    AuthenticationTokenService authenticationTokenService,
    AvatarUpdater avatarUpdater,
    ExternalAvatarClient externalAvatarClient,
    ExternalAuthenticationHelper externalAuthenticationHelper,
    ExternalAuthenticationService externalAuthenticationService,
    IHttpContextAccessor httpContextAccessor,
    IExecutionContext executionContext,
    IMediator mediator,
    ITelemetryEventsCollector events,
    TimeProvider timeProvider,
    ILogger<CompleteExternalSignupHandler> logger
) : IRequestHandler<CompleteExternalSignupCommand, Result<string>>
{
    public async Task<Result<string>> Handle(CompleteExternalSignupCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var validationResult = await externalAuthenticationHelper.ValidateCallback(
                command.Code, command.State, command.Error, command.ErrorDescription, ExternalLoginType.Signup, cancellationToken
            );

            if (!validationResult.IsSuccess) return validationResult.ErrorResult!;

            var externalLogin = validationResult.ExternalLogin;
            var userProfile = validationResult.UserProfile!;

            var existingUser = await userRepository.GetUserByEmailUnfilteredAsync(userProfile.Email, cancellationToken);
            if (existingUser is not null)
            {
                logger.LogWarning("User already exists for external login '{ExternalLoginId}'", externalLogin.Id);
                return SignupFailedRedirect(externalLogin, ExternalLoginResult.AccountAlreadyExists);
            }

            var locale = externalAuthenticationService.GetLocaleCookie() ?? userProfile.Locale;

            var createTenantResult = await mediator.Send(new CreateTenantCommand(userProfile.Email, true, locale), cancellationToken);
            if (!createTenantResult.IsSuccess)
            {
                logger.LogWarning("Failed to create tenant for external signup '{ExternalLoginId}'", externalLogin.Id);
                return SignupFailedRedirect(externalLogin, ExternalLoginResult.CodeExchangeFailed);
            }

            var user = await userRepository.GetByIdAsync(createTenantResult.Value!.UserId, cancellationToken);
            if (user is null)
            {
                logger.LogWarning("Failed to get user after tenant creation for external signup '{ExternalLoginId}'", externalLogin.Id);
                return SignupFailedRedirect(externalLogin, ExternalLoginResult.CodeExchangeFailed);
            }

            user.AddExternalIdentity(externalLogin.ProviderType, userProfile.ProviderUserId);

            if (userProfile.FirstName is not null || userProfile.LastName is not null)
            {
                user.Update(userProfile.FirstName ?? string.Empty, userProfile.LastName ?? string.Empty, string.Empty);
            }

            if (userProfile.AvatarUrl is not null)
            {
                var externalAvatar = await externalAvatarClient.DownloadAvatarAsync(userProfile.AvatarUrl, cancellationToken);
                if (externalAvatar is not null)
                {
                    await avatarUpdater.UpdateAvatar(user, false, externalAvatar.ContentType, externalAvatar.Stream, cancellationToken);
                }
            }

            userRepository.Update(user);

            externalLogin.MarkCompleted(userProfile.Email);
            externalLoginRepository.Update(externalLogin);

            var httpContext = httpContextAccessor.HttpContext!;
            var userAgent = httpContext.Request.Headers.UserAgent.ToString();
            var loginMethod = ExternalAuthenticationService.GetLoginMethod(externalLogin.ProviderType);
            var ipAddress = executionContext.ClientIpAddress;
            var session = Session.Create(user.TenantId, user.Id, loginMethod, userAgent, ipAddress);
            await sessionRepository.AddAsync(session, cancellationToken);

            user.UpdateLastSeen(timeProvider.GetUtcNow());
            userRepository.Update(user);

            var userInfoResult = await userInfoFactory.CreateUserInfoAsync(user, session.Id, cancellationToken);
            if (!userInfoResult.IsSuccess) return Result<string>.From(userInfoResult);

            authenticationTokenService.CreateAndSetAuthenticationTokens(userInfoResult.Value!, session.Id, session.RefreshTokenJti);

            events.CollectEvent(new SessionCreated(session.Id));
            var signupTimeInSeconds = (int)(timeProvider.GetUtcNow() - externalLogin.CreatedAt).TotalSeconds;
            events.CollectEvent(new ExternalSignupCompleted(createTenantResult.Value.TenantId, externalLogin.ProviderType, signupTimeInSeconds));

            var returnPath = ReturnPathHelper.GetReturnPathCookie(httpContext) ?? "/";
            ReturnPathHelper.ClearReturnPathCookie(httpContext);

            return Result<string>.Redirect(returnPath);
        }
        finally
        {
            externalAuthenticationService.ClearExternalLoginCookie();
            externalAuthenticationService.ClearLocaleCookie();
        }
    }

    private Result<string> SignupFailedRedirect(ExternalLogin externalLogin, ExternalLoginResult loginResult)
    {
        var timeInSeconds = (int)(timeProvider.GetUtcNow() - externalLogin.CreatedAt).TotalSeconds;
        if (!externalLogin.IsConsumed)
        {
            externalLogin.MarkFailed(loginResult);
            externalLoginRepository.Update(externalLogin);
        }

        events.CollectEvent(new ExternalSignupFailed(externalLogin.Id, loginResult, timeInSeconds));

        var oidcError = ExternalAuthenticationService.MapToOidcError(loginResult);
        return Result<string>.Redirect($"/error?error={oidcError}&id={externalLogin.Id}");
    }
}
