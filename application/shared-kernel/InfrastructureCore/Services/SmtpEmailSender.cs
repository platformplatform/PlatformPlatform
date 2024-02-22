using System.Net.Mail;
using PlatformPlatform.SharedKernel.ApplicationCore.Services;

namespace PlatformPlatform.SharedKernel.InfrastructureCore.Services;

public sealed class SmtpEmailSender : ISmtpEmailSender
{
    private readonly SmtpClient _emailSender = new("localhost", 1025);

    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        return _emailSender.SendMailAsync(new MailMessage("test@localhost", email, subject, htmlMessage)
        {
            IsBodyHtml = true
        });
    }
}