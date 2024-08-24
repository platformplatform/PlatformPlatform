using PlatformPlatform.AccountManagement.Application.TelemetryEvents;
using PlatformPlatform.AccountManagement.Domain.Signups;
using PlatformPlatform.SharedKernel.ApplicationCore.Authentication;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;
using PlatformPlatform.SharedKernel.ApplicationCore.TelemetryEvents;

namespace PlatformPlatform.AccountManagement.Application.Signups;

public sealed record CompleteSignupCommand(string OneTimePassword)
    : ICommand, IRequest<Result>
{
    [JsonIgnore]
    public SignupId Id { get; init; } = null!;
}

public sealed class CompleteSignupHandler(
    ITenantRepository tenantRepository,
    ISignupRepository signupRepository,
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

        if (oneTimePasswordHelper.Validate(signup.OneTimePasswordHash, command.OneTimePassword))
        {
            signup.RegisterInvalidPasswordAttempt();
            signupRepository.Update(signup);
            events.CollectEvent(new SignupFailed(signup.RetryCount));
            return Result.BadRequest("The code is wrong or no longer valid.", true);
        }

        if (signup.Completed)
        {
            logger.LogWarning("Signup with id '{SignupId}' has already been completed.", signup.Id);
            return Result.BadRequest($"The signup with id {signup.Id} for tenant {signup.TenantId} has already been completed.");
        }

        if (signup.RetryCount >= Signup.MaxAttempts)
        {
            events.CollectEvent(new SignupBlocked(signup.RetryCount));
            return Result.Forbidden("To many attempts, please request a new code.", true);
        }

        var signupTimeInSeconds = (TimeProvider.System.GetUtcNow() - signup.CreatedAt).TotalSeconds;
        if (signup.HasExpired())
        {
            events.CollectEvent(new SignupExpired((int)signupTimeInSeconds));
            return Result.BadRequest("The code is no longer valid, please request a new code.", true);
        }

        var tenant = Tenant.Create(signup.TenantId, signup.Email);
        await tenantRepository.AddAsync(tenant, cancellationToken);

        signup.MarkAsCompleted();
        signupRepository.Update(signup);

        events.CollectEvent(new SignupCompleted(tenant.Id, tenant.State, (int)signupTimeInSeconds));

        return Result.Success();
    }
}
