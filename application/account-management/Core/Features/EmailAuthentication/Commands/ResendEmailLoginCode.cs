using JetBrains.Annotations;
using Microsoft.AspNetCore.Identity;
using PlatformPlatform.AccountManagement.Features.EmailAuthentication.Domain;
using PlatformPlatform.SharedKernel.Authentication;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Integrations.Email;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.AccountManagement.Features.EmailAuthentication.Commands;

[PublicAPI]
public sealed record ResendEmailLoginCodeCommand : ICommand, IRequest<Result<ResendEmailLoginCodeResponse>>
{
    [JsonIgnore] // Removes this property from the API contract
    public EmailLoginId Id { get; init; } = null!;
}

[PublicAPI]
public sealed record ResendEmailLoginCodeResponse(int ValidForSeconds);

public sealed class ResendEmailLoginCodeHandler(
    IEmailLoginRepository emailLoginRepository,
    IEmailClient emailClient,
    IPasswordHasher<object> passwordHasher,
    ITelemetryEventsCollector events,
    TimeProvider timeProvider,
    ILogger<ResendEmailLoginCodeHandler> logger
) : IRequestHandler<ResendEmailLoginCodeCommand, Result<ResendEmailLoginCodeResponse>>
{
    public async Task<Result<ResendEmailLoginCodeResponse>> Handle(ResendEmailLoginCodeCommand codeCommand, CancellationToken cancellationToken)
    {
        var emailLogin = await emailLoginRepository.GetByIdAsync(codeCommand.Id, cancellationToken);
        if (emailLogin is null) return Result<ResendEmailLoginCodeResponse>.NotFound($"Email login with id '{codeCommand.Id}' not found.");

        if (emailLogin.Completed)
        {
            logger.LogWarning("Email login with id '{EmailLoginId}' has already been completed", emailLogin.Id);
            return Result<ResendEmailLoginCodeResponse>.BadRequest($"The email login with id '{emailLogin.Id}' has already been completed.");
        }

        if (emailLogin.ResendCount >= EmailLogin.MaxResends)
        {
            events.CollectEvent(new EmailLoginCodeResendBlocked(emailLogin.Id, emailLogin.Type, emailLogin.RetryCount));
            return Result<ResendEmailLoginCodeResponse>.Forbidden("Too many attempts, please request a new code.", true);
        }

        var oneTimePassword = OneTimePasswordHelper.GenerateOneTimePassword(6);
        var oneTimePasswordHash = passwordHasher.HashPassword(this, oneTimePassword);
        emailLogin.UpdateVerificationCode(oneTimePasswordHash, timeProvider.GetUtcNow());
        emailLoginRepository.Update(emailLogin);

        var secondsSinceStarted = (timeProvider.GetUtcNow() - emailLogin.CreatedAt).TotalSeconds;
        events.CollectEvent(new EmailLoginCodeResend((int)secondsSinceStarted));

        await emailClient.SendAsync(emailLogin.Email, "Your verification code (resend)",
            $"""
             <h1 style="text-align:center;font-family=sans-serif;font-size:20px">Here's your new verification code</h1>
             <p style="text-align:center;font-family=sans-serif;font-size:16px">We're sending this code again as you requested.</p>
             <p style="text-align:center;font-family=sans-serif;font-size:40px;background:#f5f4f5">{oneTimePassword}</p>
             <p style="text-align:center;font-family=sans-serif;font-size:14px;color:#666">This code will expire in a few minutes.</p>
             """,
            cancellationToken
        );

        return new ResendEmailLoginCodeResponse(EmailLogin.ValidForSeconds);
    }
}
