using JetBrains.Annotations;
using Microsoft.AspNetCore.Identity;
using PlatformPlatform.AccountManagement.Features.EmailConfirmations.Domain;
using PlatformPlatform.SharedKernel.Authentication;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Integrations.Email;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.AccountManagement.Features.EmailConfirmations.Commands;

[PublicAPI]
public sealed record ResendEmailConfirmationCodeCommand : ICommand, IRequest<Result<ResendEmailConfirmationCodeResponse>>
{
    [JsonIgnore] // Removes this property from the API contract
    public EmailConfirmationId Id { get; init; } = null!;
}

[PublicAPI]
public sealed record ResendEmailConfirmationCodeResponse(int ValidForSeconds);

public sealed class ResendEmailConfirmationCodeHandler(
    IEmailConfirmationRepository emailConfirmationRepository,
    IEmailClient emailClient,
    IPasswordHasher<object> passwordHasher,
    ITelemetryEventsCollector events,
    ILogger<ResendEmailConfirmationCodeHandler> logger
) : IRequestHandler<ResendEmailConfirmationCodeCommand, Result<ResendEmailConfirmationCodeResponse>>
{
    public async Task<Result<ResendEmailConfirmationCodeResponse>> Handle(ResendEmailConfirmationCodeCommand codeCommand, CancellationToken cancellationToken)
    {
        var emailConfirmation = await emailConfirmationRepository.GetByIdAsync(codeCommand.Id, cancellationToken);
        if (emailConfirmation is null) return Result<ResendEmailConfirmationCodeResponse>.NotFound($"EmailConfirmation with id '{codeCommand.Id}' not found.");

        if (emailConfirmation.Completed)
        {
            logger.LogWarning("EmailConfirmation with id '{EmailConfirmationId}' has already been completed", emailConfirmation.Id);
            return Result<ResendEmailConfirmationCodeResponse>.BadRequest($"The email confirmation with id {emailConfirmation.Id} has already been completed.");
        }

        if (emailConfirmation.ModifiedAt > TimeProvider.System.GetUtcNow().AddSeconds(-30))
        {
            return Result<ResendEmailConfirmationCodeResponse>.BadRequest("You must wait at least 30 seconds before requesting a new code.");
        }

        if (emailConfirmation.ResendCount >= EmailConfirmation.MaxResends)
        {
            events.CollectEvent(new EmailConfirmationResendBlocked(emailConfirmation.Id, emailConfirmation.Type, emailConfirmation.RetryCount));
            return Result<ResendEmailConfirmationCodeResponse>.Forbidden("Too many attempts, please request a new code.", true);
        }

        var oneTimePassword = OneTimePasswordHelper.GenerateOneTimePassword(6);
        var oneTimePasswordHash = passwordHasher.HashPassword(this, oneTimePassword);
        emailConfirmation.UpdateVerificationCode(oneTimePasswordHash);
        emailConfirmationRepository.Update(emailConfirmation);

        var secondsSinceSignupStarted = (TimeProvider.System.GetUtcNow() - emailConfirmation.CreatedAt).TotalSeconds;
        events.CollectEvent(new EmailConfirmationResend((int)secondsSinceSignupStarted));

        await emailClient.SendAsync(emailConfirmation.Email, "Your verification code (resend)",
            $"""
             <h1 style="text-align:center;font-family=sans-serif;font-size:20px">Here's your new verification code</h1>
             <p style="text-align:center;font-family=sans-serif;font-size:16px">We're sending this code again as you requested.</p>
             <p style="text-align:center;font-family=sans-serif;font-size:40px;background:#f5f4f5">{oneTimePassword}</p>
             <p style="text-align:center;font-family=sans-serif;font-size:14px;color:#666">This code will expire in a few minutes.</p>
             """,
            cancellationToken
        );

        return new ResendEmailConfirmationCodeResponse(EmailConfirmation.ValidForSeconds);
    }
}
