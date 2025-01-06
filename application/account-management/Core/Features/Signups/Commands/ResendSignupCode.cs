using JetBrains.Annotations;
using Microsoft.AspNetCore.Identity;
using PlatformPlatform.AccountManagement.Features.Signups.Domain;
using PlatformPlatform.SharedKernel.Authentication;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Integrations.Email;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.AccountManagement.Features.Signups.Commands;

[PublicAPI]
public sealed record ResendSignupCodeCommand : ICommand, IRequest<Result<ResendSignupCodeResponse>>
{
    [JsonIgnore] // Removes this property from the API contract
    public SignupId Id { get; init; } = null!;
}

[PublicAPI]
public sealed record ResendSignupCodeResponse(int ValidForSeconds);

public sealed class ResendSignupCodeCommandHandler(
    ISignupRepository signupRepository,
    IEmailClient emailClient,
    IPasswordHasher<object> passwordHasher,
    ITelemetryEventsCollector events,
    ILogger<ResendSignupCodeCommandHandler> logger
) : IRequestHandler<ResendSignupCodeCommand, Result<ResendSignupCodeResponse>>
{
    public async Task<Result<ResendSignupCodeResponse>> Handle(ResendSignupCodeCommand command, CancellationToken cancellationToken)
    {
        var signup = await signupRepository.GetByIdAsync(command.Id, cancellationToken);
        if (signup is null)
        {
            return Result<ResendSignupCodeResponse>.NotFound($"Signup with id '{command.Id}' not found.");
        }

        if (signup.Completed)
        {
            logger.LogWarning("Signup with id '{SignupId}' has already been completed.", signup.Id);
            return Result<ResendSignupCodeResponse>.BadRequest($"The signup with id {signup.Id} has already been completed.");
        }

        if (signup.ModifiedAt > TimeProvider.System.GetUtcNow().AddSeconds(-30))
        {
            return Result<ResendSignupCodeResponse>.BadRequest("You must wait at least 30 seconds before requesting a new code.");
        }

        var oneTimePassword = OneTimePasswordHelper.GenerateOneTimePassword(6);
        var oneTimePasswordHash = passwordHasher.HashPassword(this, oneTimePassword);
        signup.UpdateVerificationCode(oneTimePasswordHash);
        signupRepository.Update(signup);

        var secondsSinceSignupStarted = (TimeProvider.System.GetUtcNow() - signup.CreatedAt).TotalSeconds;
        events.CollectEvent(new SignupCodeResend(signup.TenantId, (int)secondsSinceSignupStarted));

        await emailClient.SendAsync(signup.Email, "Confirm your email address",
            $"""
             <h1 style="text-align:center;font-family=sans-serif;font-size:20px">Your confirmation code is below</h1>
             <p style="text-align:center;font-family=sans-serif;font-size:16px">Enter it in your open browser window. It is only valid for a few minutes.</p>
             <p style="text-align:center;font-family=sans-serif;font-size:40px;background:#f5f4f5">{oneTimePassword}</p>
             """,
            cancellationToken
        );


        return new ResendSignupCodeResponse(Signup.ValidForSeconds);
    }
}
