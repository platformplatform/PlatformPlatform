using Microsoft.AspNetCore.Identity;
using PlatformPlatform.SharedKernel.ApplicationCore.Services;
using IdentityUser = PlatformPlatform.AccountManagement.Infrastructure.Identity.IdentityUser;

namespace PlatformPlatform.AccountManagement.Api.Auth;

internal sealed class IdentityEmailTestSender(ISmtpEmailSender emailSender) : IEmailSender<IdentityUser>
{
    public Task SendConfirmationLinkAsync(IdentityUser user, string email, string confirmationLink)
    {
        return emailSender.SendEmailAsync(email, "Confirm your email",
            $"Please confirm your account by clicking this link: <a href='{confirmationLink}'>link</a>");
    }

    public Task SendPasswordResetLinkAsync(IdentityUser user, string email, string resetLink)
    {
        return emailSender.SendEmailAsync(email, "Reset your password",
            $"Please reset your password by clicking here: <a href='{resetLink}'>link</a>");
    }

    public Task SendPasswordResetCodeAsync(IdentityUser user, string email, string resetCode)
    {
        return emailSender.SendEmailAsync(email, "Reset your password",
            $"Please reset your password using the following code: {resetCode}");
    }
}