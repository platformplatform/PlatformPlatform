using Account.Features.Authentication.Domain;
using Account.Features.EmailAuthentication.Domain;
using Account.Features.EmailAuthentication.Shared;
using Account.Features.Tenants.Commands;
using Account.Features.Users.Domain;
using Account.Features.Users.Shared;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using SharedKernel.Authentication.TokenGeneration;
using SharedKernel.Cqrs;
using SharedKernel.ExecutionContext;
using SharedKernel.Telemetry;

namespace Account.Features.EmailAuthentication.Commands;

[PublicAPI]
public sealed record CompleteEmailSignupCommand(string OneTimePassword, string PreferredLocale) : ICommand, IRequest<Result>
{
    [JsonIgnore] // Removes this property from the API contract
    public EmailLoginId EmailLoginId { get; init; } = null!;
}

public sealed class CompleteEmailSignupHandler(
    IUserRepository userRepository,
    ISessionRepository sessionRepository,
    UserInfoFactory userInfoFactory,
    AuthenticationTokenService authenticationTokenService,
    IHttpContextAccessor httpContextAccessor,
    IExecutionContext executionContext,
    CompleteEmailConfirmation completeEmailConfirmation,
    IMediator mediator,
    ITelemetryEventsCollector events,
    TimeProvider timeProvider
) : IRequestHandler<CompleteEmailSignupCommand, Result>
{
    public async Task<Result> Handle(CompleteEmailSignupCommand command, CancellationToken cancellationToken)
    {
        var completeEmailConfirmationResult = await completeEmailConfirmation.CompleteAsync(
            command.EmailLoginId, command.OneTimePassword, cancellationToken
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

        var session = Session.Create(user!.TenantId, user.Id, LoginMethod.OneTimePassword, userAgent, ipAddress);
        await sessionRepository.AddAsync(session, cancellationToken);

        user.UpdateLastSeen(timeProvider.GetUtcNow());
        userRepository.Update(user);

        var userInfoResult = await userInfoFactory.CreateUserInfoAsync(user, session.Id, cancellationToken);
        if (!userInfoResult.IsSuccess) return Result.From(userInfoResult);

        authenticationTokenService.CreateAndSetAuthenticationTokens(userInfoResult.Value!, session.Id, session.RefreshTokenJti);

        events.CollectEvent(new SessionCreated(session.Id));
        events.CollectEvent(new SignupCompleted(createTenantResult.Value.TenantId, completeEmailConfirmationResult.Value!.ConfirmationTimeInSeconds));
        return Result.Success();
    }
}
