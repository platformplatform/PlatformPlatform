namespace SharedKernel.Integrations.Email;

public interface IEmailClient
{
    Task SendAsync(string recipient, string subject, string htmlContent, CancellationToken cancellationToken);
}
