using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Authentication.Services;
using PlatformPlatform.AccountManagement.Signups.Domain;
using PlatformPlatform.AccountManagement.TelemetryEvents;
using PlatformPlatform.AccountManagement.Tenants.Commands;
using PlatformPlatform.AccountManagement.Users.Domain;
using PlatformPlatform.SharedKernel.Authentication;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.TelemetryEvents;

namespace PlatformPlatform.AccountManagement.Signups.Commands;

[PublicAPI]
public sealed record CompleteSignupCommand(string OneTimePassword) : ICommand, IRequest<Result>
{
    [JsonIgnore] // Removes this property from the API contract
    public SignupId Id { get; init; } = null!;
}

public sealed class CompleteSignupHandler(
    ISignupRepository signupRepository,
    IUserRepository userRepository,
    AuthenticationTokenService authenticationTokenService,
    IMediator mediator,
    OneTimePasswordHelper oneTimePasswordHelper,
    ITelemetryEventsCollector events,
    ILogger<CompleteSignupHandler> logger
) : IRequestHandler<CompleteSignupCommand, Result>
{
    public async Task<Result> Handle(CompleteSignupCommand command, CancellationToken cancellationToken)
    {
        var signup = await signupRepository.GetByIdAsync(command.Id, cancellationToken);

        if (signup is null)
        {
            return Result.NotFound($"Signup with id '{command.Id}' not found.");
        }

        if (signup.Completed)
        {
            logger.LogWarning("Signup with id '{SignupId}' has already been completed.", signup.Id);
            return Result.BadRequest($"The signup with id {signup.Id} for tenant {signup.TenantId} has already been completed.");
        }

        if (signup.RetryCount >= Signup.MaxAttempts)
        {
            signup.RegisterInvalidPasswordAttempt();
            signupRepository.Update(signup);
            events.CollectEvent(new SignupBlocked(signup.RetryCount));
            return Result.Forbidden("To many attempts, please request a new code.", true);
        }

        if (oneTimePasswordHelper.Validate(signup.OneTimePasswordHash, command.OneTimePassword))
        {
            signup.RegisterInvalidPasswordAttempt();
            signupRepository.Update(signup);
            events.CollectEvent(new SignupFailed(signup.RetryCount));
            return Result.BadRequest("The code is wrong or no longer valid.", true);
        }

        var signupTimeInSeconds = (TimeProvider.System.GetUtcNow() - signup.CreatedAt).TotalSeconds;
        if (signup.HasExpired())
        {
            events.CollectEvent(new SignupExpired((int)signupTimeInSeconds));
            return Result.BadRequest("The code is no longer valid, please request a new code.", true);
        }

        var result = await mediator.Send(new CreateTenantCommand(signup.TenantId, signup.Email, true), cancellationToken);

        var user = await userRepository.GetByIdAsync(result.Value!, cancellationToken);
        authenticationTokenService.CreateAndSetAuthenticationTokens(user!);

        signup.MarkAsCompleted();
        signupRepository.Update(signup);
        events.CollectEvent(new SignupCompleted(signup.TenantId, (int)signupTimeInSeconds));

        return Result.Success();
    }
}
