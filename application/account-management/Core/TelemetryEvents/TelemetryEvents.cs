using PlatformPlatform.AccountManagement.Core.Tenants.Domain;
using PlatformPlatform.AccountManagement.Core.Users.Domain;
using PlatformPlatform.SharedKernel.ApplicationCore.TelemetryEvents;

namespace PlatformPlatform.AccountManagement.Core.TelemetryEvents;

/// This file contains all the telemetry events that are collected by the application. Telemetry events are important
/// to understand how the application is being used and collect valuable information for the business. Quality is
/// important, and keeping all the telemetry events in one place makes it easier to maintain high quality.
/// This particular includes the naming of the telemetry events (which should be in past tense) and the properties that
/// are collected with each telemetry event. Since missing or bad data cannot be fixed, it is important to have a good
/// data quality from the start.
public sealed class SignupBlocked(int retryCount)
    : TelemetryEvent(nameof(SignupBlocked), ("RetryCount", retryCount.ToString()));

public sealed class SignupCompleted(TenantId tenantId, TenantState state, int signupTimeInSeconds)
    : TelemetryEvent(nameof(SignupCompleted),
        ("TenantId", tenantId), ("TenantState", state.ToString()), ("SignupTimeInSeconds", signupTimeInSeconds.ToString())
    );

public sealed class SignupExpired(int secondsFromCreation)
    : TelemetryEvent(nameof(SignupExpired), ("SecondsFromCreation", secondsFromCreation.ToString()));

public sealed class SignupFailed(int retryCount)
    : TelemetryEvent(nameof(SignupFailed), ("RetryCount", retryCount.ToString()));

public sealed class SignupStarted(TenantId tenantId)
    : TelemetryEvent(nameof(SignupStarted), ("TenantId", tenantId));

public sealed class LoginBlocked(int retryCount)
    : TelemetryEvent(nameof(LoginBlocked), ("RetryCount", retryCount.ToString()));

public sealed class LoginCompleted(UserId userId, int loginTimeInSeconds)
    : TelemetryEvent(nameof(LoginCompleted), ("UserId", userId), ("LoginTimeInSeconds", loginTimeInSeconds.ToString()));

public sealed class Logout(UserId userId)
    : TelemetryEvent(nameof(Logout), ("UserId", userId));

public sealed class AuthenticationTokensRefreshed(UserId userId)
    : TelemetryEvent(nameof(AuthenticationTokensRefreshed), ("UserId", userId));

public sealed class LoginExpired(int secondsFromCreation)
    : TelemetryEvent(nameof(LoginExpired), ("SecondsFromCreation", secondsFromCreation.ToString()));

public sealed class LoginFailed(int retryCount)
    : TelemetryEvent(nameof(LoginFailed), ("RetryCount", retryCount.ToString()));

public sealed class LoginStarted(UserId userId)
    : TelemetryEvent(nameof(LoginStarted), ("UserId", userId));

public sealed class TenantDeleted(TenantId tenantId, TenantState tenantState)
    : TelemetryEvent(nameof(TenantDeleted), ("TenantId", tenantId), ("TenantState", tenantState.ToString()));

public sealed class TenantUpdated(TenantId tenantId)
    : TelemetryEvent(nameof(TenantUpdated), ("TenantId", tenantId));

public sealed class UserCreated(TenantId tenantId, bool gravatarProfileFound)
    : TelemetryEvent(nameof(UserCreated), ("TenantId", tenantId), ("GravatarProfileFound", gravatarProfileFound.ToString()));

public sealed class UserDeleted()
    : TelemetryEvent(nameof(UserDeleted));

public sealed class UserUpdated()
    : TelemetryEvent(nameof(UserUpdated));

public sealed class UserRoleChanged(UserRole fromRole, UserRole toRole)
    : TelemetryEvent(nameof(UserRoleChanged), ("FromRole", fromRole.ToString()), ("ToRole", toRole.ToString()));

public sealed class UserAvatarUpdated(string contentType, long size)
    : TelemetryEvent(nameof(UserAvatarUpdated), ("ContentType", contentType), ("Size", size.ToString()));

public sealed class UserAvatarRemoved()
    : TelemetryEvent(nameof(UserAvatarUpdated));
