namespace PlatformPlatform.SharedKernel.ApplicationCore.Authentication;

public class RefreshToken
{
    public const string XAccessTokenKey = "X-Access-Token";

    public const string XRefreshTokenKey = "X-Refresh-Token";

    public string TokenChainId { get; private init; } = Guid.NewGuid().ToString();

    public required string UserId { get; init; }

    public DateTimeOffset Expires { get; private init; } = TimeProvider.System.GetUtcNow().AddMonths(3);

    public int Version { get; set; } = 1;
}
