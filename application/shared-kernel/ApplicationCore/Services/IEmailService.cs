namespace PlatformPlatform.SharedKernel.ApplicationCore.Services;

public interface IEmailService
{
    [UsedImplicitly]
    Task SendAsync(string recipient, string subject, string htmlContent, CancellationToken cancellationToken);
}