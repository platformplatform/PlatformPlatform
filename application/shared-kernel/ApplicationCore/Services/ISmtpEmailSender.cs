namespace PlatformPlatform.SharedKernel.ApplicationCore.Services;

public interface ISmtpEmailSender
{
    Task SendEmailAsync(string email, string subject, string htmlMessage);
}