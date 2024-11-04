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
public sealed class AuthenticationTokensRefreshed(UserId userId)
    : TelemetryEvent(("UserId", userId));

public sealed class LoginBlocked(int retryCount)
    : TelemetryEvent(("RetryCount", retryCount));

public sealed class LoginCompleted(UserId userId, int loginTimeInSeconds)
    : TelemetryEvent(("UserId", userId), ("LoginTimeInSeconds", loginTimeInSeconds));

public sealed class LoginExpired(int secondsFromCreation)
    : TelemetryEvent(("SecondsFromCreation", secondsFromCreation));

public sealed class LoginFailed(int retryCount)
    : TelemetryEvent(("RetryCount", retryCount));

public sealed class LoginStarted(UserId userId)
    : TelemetryEvent(("UserId", userId));

public sealed class Logout(UserId userId)
    : TelemetryEvent(("UserId", userId));

public sealed class SignupBlocked(int retryCount)
    : TelemetryEvent(("RetryCount", retryCount));

public sealed class SignupCompleted(TenantId tenantId, int signupTimeInSeconds)
    : TelemetryEvent(("TenantId", tenantId), ("SignupTimeInSeconds", signupTimeInSeconds));

public sealed class SignupExpired(int secondsFromCreation)
    : TelemetryEvent(("SecondsFromCreation", secondsFromCreation));

public sealed class SignupFailed(int retryCount)
    : TelemetryEvent(("RetryCount", retryCount));

public sealed class SignupStarted(TenantId tenantId)
    : TelemetryEvent(("TenantId", tenantId));

public sealed class TenantCreated(TenantId tenantId, TenantState state)
    : TelemetryEvent(("TenantId", tenantId), ("TenantState", state));

public sealed class TenantDeleted(TenantId tenantId, TenantState tenantState)
    : TelemetryEvent(("TenantId", tenantId), ("TenantState", tenantState));

public sealed class TenantUpdated(TenantId tenantId)
    : TelemetryEvent(("TenantId", tenantId));

public sealed class UserAvatarRemoved
    : TelemetryEvent;

public sealed class UserAvatarUpdated(string contentType, long size)
    : TelemetryEvent(("ContentType", contentType), ("Size", size));

public sealed class GravatarUpdated(UserId userId, long size)
    : TelemetryEvent(("UserId", userId), ("Size", size));

public sealed class UserCreated(TenantId tenantId, bool gravatarProfileFound)
    : TelemetryEvent(("TenantId", tenantId), ("GravatarProfileFound", gravatarProfileFound));

public sealed class UserDeleted
    : TelemetryEvent;

public sealed class UserInviteAccepted(UserId userId, int inviteAcceptedTimeInMinutes)
    : TelemetryEvent(("UserId", userId), ("InviteAcceptedTimeInMinutes", inviteAcceptedTimeInMinutes));

public sealed class UserInvited
    : TelemetryEvent;

public sealed class UserLocaleChanged(UserId userId, string oldLocale, string newLocale)
    : TelemetryEvent(("UserId", userId), ("OldLocale", oldLocale), ("NewLocale", newLocale));

public sealed class UserRoleChanged(UserRole fromRole, UserRole toRole)
    : TelemetryEvent(("FromRole", fromRole), ("ToRole", toRole));

public sealed class UserUpdated
    : TelemetryEvent;
