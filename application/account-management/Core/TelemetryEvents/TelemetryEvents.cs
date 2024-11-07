using PlatformPlatform.AccountManagement.Features.Tenants.Domain;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.TelemetryEvents;

namespace PlatformPlatform.AccountManagement.TelemetryEvents;

/// This file contains all the telemetry events that are collected by the application. Telemetry events are important
/// to understand how the application is being used and collect valuable information for the business. Quality is
/// important, and keeping all the telemetry events in one place makes it easier to maintain high quality.
/// This particular includes the naming of the telemetry events (which should be in past tense) and the properties that
/// are collected with each telemetry event. Since missing or bad data cannot be fixed, it is important to have a good
/// data quality from the start.
public sealed class AuthenticationTokensRefreshed
    : TelemetryEvent;

public sealed class LoginBlocked(UserId userId, int retryCount)
    : TelemetryEvent(("UserId", userId), ("RetryCount", retryCount));

public sealed class LoginCompleted(UserId userId, int loginTimeInSeconds)
    : TelemetryEvent(("UserId", userId), ("LoginTimeInSeconds", loginTimeInSeconds));

public sealed class LoginExpired(UserId userId, int secondsFromCreation)
    : TelemetryEvent(("UserId", userId), ("SecondsFromCreation", secondsFromCreation));

public sealed class LoginFailed(UserId userId, int retryCount)
    : TelemetryEvent(("UserId", userId), ("RetryCount", retryCount));

public sealed class LoginStarted(UserId userId)
    : TelemetryEvent(("UserId", userId));

public sealed class Logout
    : TelemetryEvent;

public sealed class SignupBlocked(TenantId tenantId, int retryCount)
    : TelemetryEvent(("TenantId", tenantId), ("RetryCount", retryCount));

public sealed class SignupCompleted(TenantId tenantId, int signupTimeInSeconds)
    : TelemetryEvent(("TenantId", tenantId), ("SignupTimeInSeconds", signupTimeInSeconds));

public sealed class SignupExpired(TenantId tenantId, int secondsFromCreation)
    : TelemetryEvent(("TenantId", tenantId), ("SecondsFromCreation", secondsFromCreation));

public sealed class SignupFailed(TenantId tenantId, int retryCount)
    : TelemetryEvent(("TenantId", tenantId), ("RetryCount", retryCount));

public sealed class SignupStarted(TenantId tenantId)
    : TelemetryEvent(("TenantId", tenantId));

public sealed class TenantCreated(TenantId tenantId, TenantState state)
    : TelemetryEvent(("TenantId", tenantId), ("TenantState", state));

public sealed class TenantDeleted(TenantId tenantId, TenantState tenantState)
    : TelemetryEvent(("TenantId", tenantId), ("TenantState", tenantState));

public sealed class TenantUpdated
    : TelemetryEvent;

public sealed class UserAvatarRemoved
    : TelemetryEvent;

public sealed class UserAvatarUpdated(string contentType, long size)
    : TelemetryEvent(("ContentType", contentType), ("Size", size));

public sealed class GravatarUpdated(long size)
    : TelemetryEvent(("Size", size));

public sealed class UserCreated(UserId userId, bool gravatarProfileFound)
    : TelemetryEvent(("UserId", userId), ("GravatarProfileFound", gravatarProfileFound));

public sealed class UserDeleted(UserId userId)
    : TelemetryEvent(("UserId", userId));

public sealed class UserInviteAccepted(UserId userId, int inviteAcceptedTimeInMinutes)
    : TelemetryEvent(("UserId", userId), ("InviteAcceptedTimeInMinutes", inviteAcceptedTimeInMinutes));

public sealed class UserInvited(UserId userId)
    : TelemetryEvent(("UserId", userId));

public sealed class UserLocaleChanged(string oldLocale, string newLocale)
    : TelemetryEvent(("OldLocale", oldLocale), ("NewLocale", newLocale));

public sealed class UserRoleChanged(UserId userId, UserRole fromRole, UserRole toRole)
    : TelemetryEvent(("UserId", userId), ("FromRole", fromRole), ("ToRole", toRole));

public sealed class UserUpdated
    : TelemetryEvent;
