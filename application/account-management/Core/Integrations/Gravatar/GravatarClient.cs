using System.Net;
using System.Security.Cryptography;
using System.Text;
using PlatformPlatform.AccountManagement.Features.Users.Avatars;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.AccountManagement.TelemetryEvents;
using PlatformPlatform.SharedKernel.TelemetryEvents;

namespace PlatformPlatform.AccountManagement.Integrations.Gravatar;

public sealed class GravatarClient(
    IHttpClientFactory httpClientFactory,
    AvatarUpdater avatarUpdater,
    ITelemetryEventsCollector events,
    ILogger<GravatarClient> logger
)
{
    public async Task DownloadGravatar(User user, CancellationToken cancellationToken)
    {
        try
        {
            var hash = Convert.ToHexString(MD5.HashData(Encoding.ASCII.GetBytes(user.Email)));
            var gravatarUrl = $"https://gravatar.com/avatar/{hash.ToLowerInvariant()}?d=404";

            var gravatarHttpClient = httpClientFactory.CreateClient("Gravatar");
            gravatarHttpClient.Timeout = TimeSpan.FromSeconds(5);

            var response = await gravatarHttpClient.GetAsync(gravatarUrl, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                logger.LogInformation("No Gravatar found for user {UserId}", user.Id);
                return;
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Failed to fetch Gravatar for user {UserId}. Status Code: {StatusCode}", user.Id, response.StatusCode);
                return;
            }

            var imageStream = await response.Content.ReadAsStreamAsync(cancellationToken);

            var contentType = response.Content.Headers.ContentType?.MediaType!;
            if (await avatarUpdater.UpdateAvatar(user, true, contentType, imageStream, cancellationToken))
            {
                logger.LogInformation("Gravatar updated successfully for user {UserId}", user.Id);

                events.CollectEvent(new GravatarUpdated(user.Id, imageStream.Length));
            }
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, "Timeout when fetching gravatar  for user {UserId}", user.Id);
        }
    }
}
