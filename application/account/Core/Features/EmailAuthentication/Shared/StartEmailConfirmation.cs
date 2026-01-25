using Microsoft.AspNetCore.Identity;
using PlatformPlatform.Account.Features.EmailAuthentication.Domain;
using PlatformPlatform.SharedKernel.Authentication;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Integrations.Email;

namespace PlatformPlatform.Account.Features.EmailAuthentication.Shared;

public sealed class StartEmailConfirmation(
    IEmailLoginRepository emailLoginRepository,
    IEmailClient emailClient,
    IPasswordHasher<object> passwordHasher,
    TimeProvider timeProvider
)
{
    public async Task<Result<EmailLoginId>> StartAsync(
        string email,
        string emailSubject,
        string emailBody,
        EmailLoginType type,
        CancellationToken cancellationToken
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(emailSubject);
        if (!emailBody.Contains("{oneTimePassword}"))
        {
            throw new ArgumentException("Email body must contain {oneTimePassword} placeholder.", nameof(emailBody));
        }

        var existingLogins = emailLoginRepository.GetByEmail(email).ToArray();

        var lockoutMinutes = type == EmailLoginType.Signup ? -60 : -15;
        if (existingLogins.Count(r => r.CreatedAt > timeProvider.GetUtcNow().AddMinutes(lockoutMinutes)) >= 3)
        {
            return Result<EmailLoginId>.TooManyRequests("Too many attempts to confirm this email address. Please try again later.");
        }

        var oneTimePassword = OneTimePasswordHelper.GenerateOneTimePassword(6);
        var oneTimePasswordHash = passwordHasher.HashPassword(this, oneTimePassword);
        var emailLogin = EmailLogin.Create(email, oneTimePasswordHash, type);

        await emailLoginRepository.AddAsync(emailLogin, cancellationToken);

        var htmlContent = emailBody.Replace("{oneTimePassword}", oneTimePassword);
        await emailClient.SendAsync(emailLogin.Email, emailSubject, htmlContent, cancellationToken);

        return emailLogin.Id;
    }
}
