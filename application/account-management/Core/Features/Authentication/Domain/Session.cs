using System.Net;
using PlatformPlatform.SharedKernel.Authentication.TokenGeneration;
using PlatformPlatform.SharedKernel.Domain;

namespace PlatformPlatform.AccountManagement.Features.Authentication.Domain;

public sealed class Session : AggregateRoot<SessionId>, ITenantScopedEntity
{
    // Grace period must match DEFAULT_TIMEOUT in shared-webapp/infrastructure/http/httpClient.ts.
    // Allows in-flight requests using the previous token version to complete when a parallel request triggers a token refresh.
    public const int GracePeriodSeconds = 30;

    private Session(TenantId tenantId, UserId userId, DeviceType deviceType, string userAgent, string ipAddress)
        : base(SessionId.NewId())
    {
        TenantId = tenantId;
        UserId = userId;
        RefreshTokenJti = RefreshTokenJti.NewId();
        RefreshTokenVersion = 1;
        DeviceType = deviceType;
        UserAgent = userAgent;
        IpAddress = ipAddress;
    }

    public UserId UserId { get; private init; }

    public RefreshTokenJti RefreshTokenJti { get; private set; }

    public RefreshTokenJti? PreviousRefreshTokenJti { get; private set; }

    public int RefreshTokenVersion { get; private set; }

    public DeviceType DeviceType { get; private init; }

    public string UserAgent { get; private init; }

    public string IpAddress { get; private init; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public SessionRevokedReason? RevokedReason { get; private set; }

    public bool IsRevoked => RevokedAt is not null;

    public DateTimeOffset ExpiresAt => CreatedAt.AddHours(RefreshTokenGenerator.ValidForHours);

    public TenantId TenantId { get; }

    public static Session Create(TenantId tenantId, UserId userId, string userAgent, IPAddress ipAddress)
    {
        var deviceType = ParseDeviceType(userAgent);
        return new Session(tenantId, userId, deviceType, userAgent, ipAddress.ToString());
    }

    public void Refresh()
    {
        PreviousRefreshTokenJti = RefreshTokenJti;
        RefreshTokenJti = RefreshTokenJti.NewId();
        RefreshTokenVersion++;
    }

    public void Revoke(DateTimeOffset now, SessionRevokedReason reason)
    {
        if (IsRevoked) throw new UnreachableException("Session is already revoked.");
        RevokedAt = now;
        RevokedReason = reason;
    }

    public bool IsRefreshTokenValid(RefreshTokenJti jti, int tokenVersion, DateTimeOffset now)
    {
        if (jti == RefreshTokenJti && tokenVersion == RefreshTokenVersion)
        {
            return true;
        }

        if (jti == PreviousRefreshTokenJti && tokenVersion == RefreshTokenVersion - 1 && now <= ModifiedAt!.Value.AddSeconds(GracePeriodSeconds))
        {
            return true;
        }

        return false;
    }

    private static DeviceType ParseDeviceType(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
        {
            return DeviceType.Unknown;
        }

        var lowerUserAgent = userAgent.ToLowerInvariant();

        if (lowerUserAgent.Contains("mobile") || lowerUserAgent.Contains("iphone") || (lowerUserAgent.Contains("android") && !lowerUserAgent.Contains("tablet")))
        {
            return DeviceType.Mobile;
        }

        if (lowerUserAgent.Contains("tablet") || lowerUserAgent.Contains("ipad"))
        {
            return DeviceType.Tablet;
        }

        if (lowerUserAgent.Contains("windows") || lowerUserAgent.Contains("macintosh") || lowerUserAgent.Contains("linux"))
        {
            return DeviceType.Desktop;
        }

        return DeviceType.Unknown;
    }
}
