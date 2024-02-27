using Azure;
using Azure.Communication.Email;
using Azure.Security.KeyVault.Secrets;
using PlatformPlatform.SharedKernel.ApplicationCore.Services;

namespace PlatformPlatform.SharedKernel.InfrastructureCore.Services;

public sealed class AzureEmailService(SecretClient secretClient) : IEmailService
{
    private const string SecretName = "communication-services-connection-string";

    private static readonly string Sender = Environment.GetEnvironmentVariable("SENDER_EMAIL_ADDRESS")!;

    public async Task SendAsync(
        string recipient,
        string subject,
        string htmlContent,
        CancellationToken cancellationToken
    )
    {
        var connectionString = await secretClient.GetSecretAsync(SecretName, cancellationToken: cancellationToken);

        var emailClient = new EmailClient(connectionString.Value.Value);
        EmailMessage message = new(Sender, recipient, new EmailContent(subject) { Html = htmlContent });
        await emailClient.SendAsync(WaitUntil.Completed, message, cancellationToken);
    }
}