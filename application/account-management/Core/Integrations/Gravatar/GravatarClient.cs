using System.Net;
using System.Security.Cryptography;
using System.Text;
using PlatformPlatform.SharedKernel.Domain;

namespace PlatformPlatform.AccountManagement.Integrations.Gravatar;

public sealed record Gravatar(Stream Stream, string ContentType);

public sealed class GravatarClient(HttpClient httpClient, ILogger<GravatarClient> logger)
{
    public async Task<Gravatar?> GetGravatar(UserId userId, string email, CancellationToken cancellationToken)
    {
        try
        {
            var hash = Convert.ToHexString(MD5.HashData(Encoding.ASCII.GetBytes(email)));
            var gravatarUrl = $"avatar/{hash.ToLowerInvariant()}?d=404";

            var response = await httpClient.GetAsync(gravatarUrl, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                logger.LogInformation("No Gravatar found for user {UserId}", userId);
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Failed to fetch Gravatar for user {UserId}. Status Code: {StatusCode}", userId, response.StatusCode);
                return null;
            }

            return new Gravatar(
                await response.Content.ReadAsStreamAsync(cancellationToken),
                response.Content.Headers.ContentType?.MediaType!
            );
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, "Timeout when fetching gravatar for user {UserId}", userId);
            return null;
        }
    }
}
