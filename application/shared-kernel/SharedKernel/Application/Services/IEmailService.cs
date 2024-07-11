namespace PlatformPlatform.SharedKernel.Application.Services;

public interface IEmailService
{
    Task SendAsync(string recipient, string subject, string htmlContent, CancellationToken cancellationToken);
}
