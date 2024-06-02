namespace PlatformPlatform.SharedKernel.ApiCore.Authentication;

public class RefreshToken
{
    public string TokenChainId { get; private init; } = Guid.NewGuid().ToString();

    public DateTimeOffset Expires { get; private init; } = TimeProvider.System.GetUtcNow().AddMonths(3);

    public int Version { get; set; } = 1;
}
