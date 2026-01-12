namespace PlatformPlatform.SharedKernel.Authentication;

/// <summary>
///     Represents why a request was rejected with 401 Unauthorized.
///     Used for the x-unauthorized-reason HTTP header to communicate specific reasons to clients.
///     Session revocation reasons map directly from SessionRevokedReason; SessionNotFound
///     indicates the session could not be found (no database record to store this on).
///     IMPORTANT: Must be kept in sync with UnauthorizedReason const in
///     shared-webapp/infrastructure/auth/AuthenticationMiddleware.ts
/// </summary>
public enum UnauthorizedReason
{
    Revoked,
    ReplayAttackDetected,
    SessionNotFound
}
