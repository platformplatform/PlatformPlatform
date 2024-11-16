using PlatformPlatform.AccountManagement.Features.Tenants.Domain;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.AccountManagement.Features;

/// This file contains all the telemetry events that are collected by the application. Telemetry events are important
/// to understand how the application is being used and collect valuable information for the business. Quality is
/// important, and keeping all the telemetry events in one place makes it easier to maintain high quality.
/// This particular includes the naming of the telemetry events (which should be in past tense) and the properties that
/// are collected with each telemetry event. Since missing or bad data cannot be fixed, it is important to have a good
/// data quality from the start.
public sealed class AuthenticationTokensRefreshed
    : TelemetryEvent;

public sealed class LoginBlocked(UserId userId, int retryCount)
    : TelemetryEvent(("user_id", userId), ("retry_count", retryCount));

public sealed class LoginCompleted(UserId userId, int loginTimeInSeconds)
    : TelemetryEvent(("user_id", userId), ("login_time_in_seconds", loginTimeInSeconds));

public sealed class LoginExpired(UserId userId, int secondsFromCreation)
    : TelemetryEvent(("user_id", userId), ("seconds_from_creation", secondsFromCreation));

public sealed class LoginFailed(UserId userId, int retryCount)
    : TelemetryEvent(("user_id", userId), ("retry_count", retryCount));

public sealed class LoginStarted(UserId userId)
    : TelemetryEvent(("user_id", userId));

public sealed class Logout
    : TelemetryEvent;

public sealed class SignupBlocked(TenantId tenantId, int retryCount)
    : TelemetryEvent(("tenant_id", tenantId), ("retry_count", retryCount));

public sealed class SignupCompleted(TenantId tenantId, int signupTimeInSeconds)
    : TelemetryEvent(("tenant_id", tenantId), ("signup_time_in_seconds", signupTimeInSeconds));

public sealed class SignupExpired(TenantId tenantId, int secondsFromCreation)
    : TelemetryEvent(("tenant_id", tenantId), ("seconds_from_creation", secondsFromCreation));

public sealed class SignupFailed(TenantId tenantId, int retryCount)
    : TelemetryEvent(("tenant_id", tenantId), ("retry_count", retryCount));

public sealed class SignupStarted(TenantId tenantId)
    : TelemetryEvent(("tenant_id", tenantId));

public sealed class TenantCreated(TenantId tenantId, TenantState state)
    : TelemetryEvent(("tenant_id", tenantId), ("tenant_state", state));

public sealed class TenantDeleted(TenantId tenantId, TenantState tenantState)
    : TelemetryEvent(("tenant_id", tenantId), ("tenant_state", tenantState));

public sealed class TenantUpdated
    : TelemetryEvent;

public sealed class UserAvatarRemoved
    : TelemetryEvent;

public sealed class UserAvatarUpdated(string contentType, long size)
    : TelemetryEvent(("content_type", contentType), ("size", size));

public sealed class GravatarUpdated(long size)
    : TelemetryEvent(("size", size));

public sealed class UserCreated(UserId userId, bool gravatarProfileFound)
    : TelemetryEvent(("user_id", userId), ("gravatar_profile_found", gravatarProfileFound));

public sealed class UserDeleted(UserId userId)
    : TelemetryEvent(("user_id", userId));

public sealed class UserInviteAccepted(UserId userId, int inviteAcceptedTimeInMinutes)
    : TelemetryEvent(("user_id", userId), ("invite_accepted_time_in_minutes", inviteAcceptedTimeInMinutes));

public sealed class UserInvited(UserId userId)
    : TelemetryEvent(("user_id", userId));

public sealed class UserLocaleChanged(string fromLocale, string toLocale)
    : TelemetryEvent(("from_locale", fromLocale), ("to_locale", toLocale));

public sealed class UserRoleChanged(UserId userId, UserRole fromRole, UserRole toRole)
    : TelemetryEvent(("user_id", userId), ("from_role", fromRole), ("to_role", toRole));

public sealed class UserUpdated
    : TelemetryEvent;
