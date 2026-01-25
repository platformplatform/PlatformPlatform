using System.Net;
using JetBrains.Annotations;
using PlatformPlatform.SharedKernel.Authentication.TokenGeneration;
using PlatformPlatform.SharedKernel.Domain;

namespace PlatformPlatform.Account.Features.Authentication.Domain;

public sealed class Session : AggregateRoot<SessionId>, ITenantScopedEntity
{
    // Grace period must match DEFAULT_TIMEOUT in shared-webapp/infrastructure/http/httpClient.ts.
    // Allows in-flight requests using the previous token version to complete when a parallel request triggers a token refresh.
    public const int GracePeriodSeconds = 30;

    private Session(TenantId tenantId, UserId userId, LoginMethod loginMethod, DeviceType deviceType, string userAgent, string ipAddress)
        : base(SessionId.NewId())
    {
        TenantId = tenantId;
        UserId = userId;
        RefreshTokenJti = RefreshTokenJti.NewId();
        RefreshTokenVersion = 1;
        LoginMethod = loginMethod;
        DeviceType = deviceType;
        UserAgent = userAgent;
        IpAddress = ipAddress;
    }

    public UserId UserId { get; private init; }

    [UsedImplicitly] // Updated via raw SQL in SessionRepository.TryRefreshAsync to handle concurrent refresh requests atomically
    public RefreshTokenJti RefreshTokenJti { get; private set; }

    [UsedImplicitly] // Updated via raw SQL in SessionRepository.TryRefreshAsync
    public RefreshTokenJti? PreviousRefreshTokenJti { get; private set; }

    [UsedImplicitly] // Updated via raw SQL in SessionRepository.TryRefreshAsync
    public int RefreshTokenVersion { get; private set; }

    public LoginMethod LoginMethod { get; private init; }

    public DeviceType DeviceType { get; private init; }

    public string UserAgent { get; private init; }

    public string IpAddress { get; private init; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public SessionRevokedReason? RevokedReason { get; private set; }

    public bool IsRevoked => RevokedAt is not null;

    public DateTimeOffset ExpiresAt => CreatedAt.AddHours(RefreshTokenGenerator.ValidForHours);

    public TenantId TenantId { get; }

    public static Session Create(TenantId tenantId, UserId userId, LoginMethod loginMethod, string userAgent, IPAddress ipAddress)
    {
        var deviceType = ParseDeviceType(userAgent);
        return new Session(tenantId, userId, loginMethod, deviceType, userAgent, ipAddress.ToString());
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
