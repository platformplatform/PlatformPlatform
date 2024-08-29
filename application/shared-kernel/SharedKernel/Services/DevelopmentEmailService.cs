using System.Net.Mail;

namespace PlatformPlatform.SharedKernel.Services;

public sealed class DevelopmentEmailService : IEmailService
{
    private const string Sender = "no-reply@localhost";
    private readonly SmtpClient _emailSender = new("localhost", 9004);

    public Task SendAsync(string recipient, string subject, string htmlContent, CancellationToken cancellationToken)
    {
        var mailMessage = new MailMessage(Sender, recipient, subject, htmlContent) { IsBodyHtml = true };
        return _emailSender.SendMailAsync(mailMessage, cancellationToken);
    }
}
