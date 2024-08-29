namespace PlatformPlatform.SharedKernel.Services;

public interface IEmailService
{
    Task SendAsync(string recipient, string subject, string htmlContent, CancellationToken cancellationToken);
}
