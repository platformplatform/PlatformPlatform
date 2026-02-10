namespace PlatformPlatform.AccountManagement.Integrations.OAuth;

public sealed record ExternalAvatar(Stream Stream, string ContentType);

public sealed class ExternalAvatarClient(HttpClient httpClient, ILogger<ExternalAvatarClient> logger)
{
    private const long MaxAvatarSizeInBytes = 1024 * 1024;

    private static readonly string[] AllowedDomainSuffixes = [".googleusercontent.com", ".gravatar.com"];

    public async Task<ExternalAvatar?> DownloadAvatarAsync(string avatarUrl, CancellationToken cancellationToken)
    {
        if (!IsAllowedDomain(avatarUrl))
        {
            logger.LogWarning("Avatar URL '{AvatarUrl}' is not from an allowlisted domain, skipping download", avatarUrl);
            return null;
        }

        try
        {
            using var response = await httpClient.GetAsync(avatarUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Failed to download external avatar from '{AvatarUrl}', status '{StatusCode}'", avatarUrl, response.StatusCode);
                return null;
            }

            var contentType = response.Content.Headers.ContentType?.MediaType;
            if (contentType is null || !contentType.StartsWith("image/", StringComparison.Ordinal))
            {
                logger.LogWarning("External avatar from '{AvatarUrl}' has unexpected content type '{ContentType}'", avatarUrl, contentType);
                return null;
            }

            if (response.Content.Headers.ContentLength > MaxAvatarSizeInBytes)
            {
                logger.LogWarning("External avatar from '{AvatarUrl}' exceeds maximum size, content length '{ContentLength}'", avatarUrl, response.Content.Headers.ContentLength);
                return null;
            }

            var avatarBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            if (avatarBytes.Length > MaxAvatarSizeInBytes)
            {
                logger.LogWarning("External avatar from '{AvatarUrl}' exceeds maximum size, read '{BytesRead}' bytes", avatarUrl, avatarBytes.Length);
                return null;
            }

            return new ExternalAvatar(new MemoryStream(avatarBytes), contentType);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Timeout when downloading external avatar from '{AvatarUrl}'", avatarUrl);
            return null;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Failed to download external avatar from '{AvatarUrl}'", avatarUrl);
            return null;
        }
    }

    private static bool IsAllowedDomain(string avatarUrl)
    {
        if (!Uri.TryCreate(avatarUrl, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (uri.Scheme is not "https")
        {
            return false;
        }

        var host = uri.Host;
        return AllowedDomainSuffixes.Any(suffix => host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
    }
}
