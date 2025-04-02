using PlatformPlatform.AccountManagement.Features.EmailConfirmations.Domain;
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

public sealed class EmailConfirmationBlocked(EmailConfirmationId emailConfirmationId, EmailConfirmationType emailConfirmationType, int retryCount)
    : TelemetryEvent(("email_confirmation_id", emailConfirmationId), ("email_confirmation_type", emailConfirmationType), ("retry_count", retryCount));

public sealed class EmailConfirmationExpired(EmailConfirmationId emailConfirmationId, EmailConfirmationType emailConfirmationType, int timeInSeconds)
    : TelemetryEvent(("email_confirmation_id", emailConfirmationId), ("email_confirmation_type", emailConfirmationType), ("time_in_seconds", timeInSeconds));

public sealed class EmailConfirmationFailed(EmailConfirmationId emailConfirmationId, EmailConfirmationType emailConfirmationType, int retryCount)
    : TelemetryEvent(("email_confirmation_id", emailConfirmationId), ("email_confirmation_type", emailConfirmationType), ("retry_count", retryCount));

public sealed class EmailConfirmationResend(int secondsSinceSignupStarted)
    : TelemetryEvent(("seconds_since_signup_started", secondsSinceSignupStarted));

public sealed class EmailConfirmationResendBlocked(EmailConfirmationId emailConfirmationId, EmailConfirmationType emailConfirmationType, int resendCount)
    : TelemetryEvent(("email_confirmation_id", emailConfirmationId), ("email_confirmation_type", emailConfirmationType), ("resend_count", resendCount));

public sealed class LoginCompleted(UserId userId, int loginTimeInSeconds)
    : TelemetryEvent(("user_id", userId), ("login_time_in_seconds", loginTimeInSeconds));

public sealed class LoginStarted(UserId userId)
    : TelemetryEvent(("user_id", userId));

public sealed class Logout
    : TelemetryEvent;

public sealed class SignupCompleted(TenantId tenantId, int signupTimeInSeconds)
    : TelemetryEvent(("tenant_id", tenantId), ("signup_time_in_seconds", signupTimeInSeconds));

public sealed class SignupStarted
    : TelemetryEvent;

public sealed class TenantCreated(TenantId tenantId, TenantState state)
    : TelemetryEvent(("tenant_id", tenantId), ("tenant_state", state));

public sealed class TenantDeleted(TenantId tenantId, TenantState tenantState, int usersDeleted)
    : TelemetryEvent(("tenant_id", tenantId), ("tenant_state", tenantState), ("users_deleted", usersDeleted));

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

public sealed class UserDeleted(UserId userId, bool bulkDeletion = false)
    : TelemetryEvent(("user_id", userId), ("bulk_deletion", bulkDeletion));

public sealed class UsersBulkDeleted(int count)
    : TelemetryEvent(("count", count));

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
