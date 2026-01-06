using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using PlatformPlatform.AccountManagement.Features.Authentication.Domain;
using PlatformPlatform.AccountManagement.Features.EmailConfirmations.Commands;
using PlatformPlatform.AccountManagement.Features.EmailConfirmations.Domain;
using PlatformPlatform.AccountManagement.Features.Tenants.Commands;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.AccountManagement.Features.Users.Shared;
using PlatformPlatform.SharedKernel.Authentication.TokenGeneration;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.AccountManagement.Features.Signups.Commands;

[PublicAPI]
public sealed record CompleteSignupCommand(string OneTimePassword, string PreferredLocale) : ICommand, IRequest<Result>
{
    [JsonIgnore] // Removes this property from the API contract
    public EmailConfirmationId EmailConfirmationId { get; init; } = null!;
}

public sealed class CompleteSignupHandler(
    IUserRepository userRepository,
    ISessionRepository sessionRepository,
    UserInfoFactory userInfoFactory,
    AuthenticationTokenService authenticationTokenService,
    IHttpContextAccessor httpContextAccessor,
    IExecutionContext executionContext,
    IMediator mediator,
    ITelemetryEventsCollector events
) : IRequestHandler<CompleteSignupCommand, Result>
{
    public async Task<Result> Handle(CompleteSignupCommand command, CancellationToken cancellationToken)
    {
        var completeEmailConfirmationResult = await mediator.Send(
            new CompleteEmailConfirmationCommand(command.EmailConfirmationId, command.OneTimePassword),
            cancellationToken
        );

        if (!completeEmailConfirmationResult.IsSuccess) return Result.From(completeEmailConfirmationResult);

        var createTenantResult = await mediator.Send(
            new CreateTenantCommand(completeEmailConfirmationResult.Value!.Email, true, command.PreferredLocale),
            cancellationToken
        );

        if (!createTenantResult.IsSuccess) return Result.From(createTenantResult);

        var user = await userRepository.GetByIdAsync(createTenantResult.Value!.UserId, cancellationToken);

        var userAgent = httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString() ?? string.Empty;
        var ipAddress = executionContext.ClientIpAddress;

        var session = Session.Create(user!.TenantId, user.Id, userAgent, ipAddress);
        await sessionRepository.AddAsync(session, cancellationToken);

        var userInfo = await userInfoFactory.CreateUserInfoAsync(user, cancellationToken, session.Id);
        authenticationTokenService.CreateAndSetAuthenticationTokens(userInfo, session.Id, session.RefreshTokenJti);

        events.CollectEvent(new SessionCreated(session.Id));
        events.CollectEvent(new SignupCompleted(createTenantResult.Value.TenantId, completeEmailConfirmationResult.Value!.ConfirmationTimeInSeconds));
        return Result.Success();
    }
}
