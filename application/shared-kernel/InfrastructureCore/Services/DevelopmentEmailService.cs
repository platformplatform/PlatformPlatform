using System.Net.Mail;
using PlatformPlatform.SharedKernel.ApplicationCore.Services;

namespace PlatformPlatform.SharedKernel.InfrastructureCore.Services;

public sealed class DevelopmentEmailService : IEmailService
{
    private const string Sender = "no-reply@localhost";
    private readonly SmtpClient _emailSender = new("localhost", 1025);

    public Task SendAsync(
        string recipient,
        string subject,
        string htmlContent,
        CancellationToken cancellationToken
    )
    {
        var mailMessage = new MailMessage(Sender, recipient, subject, htmlContent) { IsBodyHtml = true };
        return _emailSender.SendMailAsync(mailMessage, cancellationToken);
    }
}