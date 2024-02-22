namespace PlatformPlatform.SharedKernel.ApplicationCore.Services;

public interface IEmailService
{
    Task SendAsync(string recipient, string subject, string htmlContent, CancellationToken cancellationToken);
}