using PlatformPlatform.Account.Features.Authentication.Domain;
using PlatformPlatform.Account.Features.EmailAuthentication.Domain;
using PlatformPlatform.Account.Features.ExternalAuthentication.Domain;
using PlatformPlatform.Account.Features.Subscriptions.Domain;
using PlatformPlatform.Account.Features.Tenants.Domain;
using PlatformPlatform.Account.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Authentication.TokenGeneration;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.Account.Features;

/// This file contains all the telemetry events that are collected by the application. Telemetry events are important
/// to understand how the application is being used and collect valuable information for the business. Quality is
/// important, and keeping all the telemetry events in one place makes it easier to maintain high quality.
/// This particular includes the naming of the telemetry events (which should be in past tense) and the properties that
/// are collected with each telemetry event. Since missing or bad data cannot be fixed, it is important to have a good
/// data quality from the start.
public sealed class BillingInfoAdded(SubscriptionId subscriptionId, string? country, string? postalCode, string? city)
    : TelemetryEvent(("subscription_id", subscriptionId), ("country", country as object ?? "unknown"), ("postal_code", postalCode as object ?? "unknown"), ("city", city as object ?? "unknown"));

public sealed class BillingInfoUpdated(SubscriptionId subscriptionId, string? country, string? postalCode, string? city)
    : TelemetryEvent(("subscription_id", subscriptionId), ("country", country as object ?? "unknown"), ("postal_code", postalCode as object ?? "unknown"), ("city", city as object ?? "unknown"));

public sealed class EmailLoginCodeBlocked(EmailLoginId emailLoginId, EmailLoginType emailLoginType, int retryCount)
    : TelemetryEvent(("email_login_id", emailLoginId), ("email_login_type", emailLoginType), ("retry_count", retryCount));

public sealed class EmailLoginCodeExpired(EmailLoginId emailLoginId, EmailLoginType emailLoginType, int timeInSeconds)
    : TelemetryEvent(("email_login_id", emailLoginId), ("email_login_type", emailLoginType), ("time_in_seconds", timeInSeconds));

public sealed class EmailLoginCodeFailed(EmailLoginId emailLoginId, EmailLoginType emailLoginType, int retryCount)
    : TelemetryEvent(("email_login_id", emailLoginId), ("email_login_type", emailLoginType), ("retry_count", retryCount));

public sealed class EmailLoginCodeResend(int secondsSinceStarted)
    : TelemetryEvent(("seconds_since_started", secondsSinceStarted));

public sealed class EmailLoginCodeResendBlocked(EmailLoginId emailLoginId, EmailLoginType emailLoginType, int resendCount)
    : TelemetryEvent(("email_login_id", emailLoginId), ("email_login_type", emailLoginType), ("resend_count", resendCount));

public sealed class EmailLoginCompleted(UserId userId, int loginTimeInSeconds)
    : TelemetryEvent(("user_id", userId), ("login_time_in_seconds", loginTimeInSeconds));

public sealed class EmailLoginStarted(UserId userId)
    : TelemetryEvent(("user_id", userId));

public sealed class ExternalLoginCompleted(UserId userId, ExternalProviderType providerType, int loginTimeInSeconds)
    : TelemetryEvent(("user_id", userId), ("provider_type", providerType), ("login_time_in_seconds", loginTimeInSeconds));

public sealed class ExternalLoginFailed(ExternalLoginId? externalLoginId, ExternalLoginResult loginResult, int timeInSeconds, string? oauthError = null)
    : TelemetryEvent(("external_login_id", externalLoginId as object ?? "unknown"), ("login_result", loginResult), ("time_in_seconds", timeInSeconds), ("oauth_error", oauthError as object ?? "none"));

public sealed class ExternalLoginStarted(ExternalProviderType providerType)
    : TelemetryEvent(("provider_type", providerType));

public sealed class ExternalSignupCompleted(TenantId tenantId, ExternalProviderType providerType, int signupTimeInSeconds)
    : TelemetryEvent(("tenant_id", tenantId), ("provider_type", providerType), ("signup_time_in_seconds", signupTimeInSeconds));

public sealed class ExternalSignupFailed(ExternalLoginId? externalLoginId, ExternalLoginResult loginResult, int timeInSeconds, string? oauthError = null)
    : TelemetryEvent(("external_login_id", externalLoginId as object ?? "unknown"), ("login_result", loginResult), ("time_in_seconds", timeInSeconds), ("oauth_error", oauthError as object ?? "none"));

public sealed class ExternalSignupStarted(ExternalProviderType providerType)
    : TelemetryEvent(("provider_type", providerType));

public sealed class GravatarUpdated(long size)
    : TelemetryEvent(("size", size));

public sealed class Logout
    : TelemetryEvent;

public sealed class PaymentFailed(SubscriptionId subscriptionId, SubscriptionPlan plan, decimal priceAmount, string currency)
    : TelemetryEvent(("subscription_id", subscriptionId), ("plan", plan), ("price_amount", priceAmount), ("currency", currency));

public sealed class PaymentMethodSetupStarted(SubscriptionId subscriptionId)
    : TelemetryEvent(("subscription_id", subscriptionId));

public sealed class PaymentMethodUpdated(SubscriptionId subscriptionId)
    : TelemetryEvent(("subscription_id", subscriptionId));

public sealed class PaymentRecovered(SubscriptionId subscriptionId, SubscriptionPlan plan, int daysInPastDue, decimal priceAmount, string currency)
    : TelemetryEvent(("subscription_id", subscriptionId), ("plan", plan), ("days_in_past_due", daysInPastDue), ("price_amount", priceAmount), ("currency", currency));

public sealed class PaymentRefunded(SubscriptionId subscriptionId, SubscriptionPlan plan, int refundCount, decimal priceAmount, string currency)
    : TelemetryEvent(("subscription_id", subscriptionId), ("plan", plan), ("refund_count", refundCount), ("price_amount", priceAmount), ("currency", currency));

public sealed class PendingInvoicePaymentRetried(SubscriptionId subscriptionId)
    : TelemetryEvent(("subscription_id", subscriptionId));

public sealed class SessionCreated(SessionId sessionId)
    : TelemetryEvent(("session_id", sessionId));

public sealed class SessionReplayDetected(SessionId sessionId, int tokenVersion, int currentVersion)
    : TelemetryEvent(("session_id", sessionId), ("token_version", tokenVersion), ("current_version", currentVersion));

public sealed class SessionRevoked(SessionRevokedReason reason, int count = 1)
    : TelemetryEvent(("reason", reason), ("count", count));

public sealed class SignupCompleted(TenantId tenantId, int signupTimeInSeconds)
    : TelemetryEvent(("tenant_id", tenantId), ("signup_time_in_seconds", signupTimeInSeconds));

public sealed class SignupStarted
    : TelemetryEvent;

public sealed class SubscriptionCancelled(
    SubscriptionId subscriptionId,
    SubscriptionPlan plan,
    CancellationReason reason,
    int? daysUntilExpiry,
    int daysOnCurrentPlan,
    decimal priceAmount,
    decimal mrrImpact,
    string currency
)
    : TelemetryEvent(("subscription_id", subscriptionId), ("plan", plan), ("reason", reason), ("days_until_expiry", daysUntilExpiry), ("days_on_current_plan", daysOnCurrentPlan), ("price_amount", priceAmount), ("mrr_impact", mrrImpact), ("currency", currency));

public sealed class SubscriptionCheckoutStarted(SubscriptionId subscriptionId, SubscriptionPlan plan, bool useExistingPaymentMethod)
    : TelemetryEvent(("subscription_id", subscriptionId), ("plan", plan), ("use_existing_payment_method", useExistingPaymentMethod));

public sealed class SubscriptionCreated(SubscriptionId subscriptionId, SubscriptionPlan plan, decimal priceAmount, decimal mrrImpact, string currency)
    : TelemetryEvent(("subscription_id", subscriptionId), ("plan", plan), ("price_amount", priceAmount), ("mrr_impact", mrrImpact), ("currency", currency));

public sealed class SubscriptionDowngradeCancelled(
    SubscriptionId subscriptionId,
    SubscriptionPlan plan,
    SubscriptionPlan scheduledPlan,
    int? daysUntilDowngrade,
    int daysSinceDowngradeScheduled,
    decimal priceAmount,
    decimal mrrImpact,
    string currency
)
    : TelemetryEvent(("subscription_id", subscriptionId), ("plan", plan), ("scheduled_plan", scheduledPlan), ("days_until_downgrade", daysUntilDowngrade), ("days_since_downgrade_scheduled", daysSinceDowngradeScheduled), ("price_amount", priceAmount), ("mrr_impact", mrrImpact), ("currency", currency));

public sealed class SubscriptionDowngraded(
    SubscriptionId subscriptionId,
    SubscriptionPlan fromPlan,
    SubscriptionPlan toPlan,
    int daysOnCurrentPlan,
    decimal previousPriceAmount,
    decimal newPriceAmount,
    decimal mrrImpact,
    string currency
)
    : TelemetryEvent(("subscription_id", subscriptionId), ("from_plan", fromPlan), ("to_plan", toPlan), ("days_on_current_plan", daysOnCurrentPlan), ("previous_price_amount", previousPriceAmount), ("new_price_amount", newPriceAmount), ("mrr_impact", mrrImpact), ("currency", currency));

public sealed class SubscriptionDowngradeScheduled(
    SubscriptionId subscriptionId,
    SubscriptionPlan fromPlan,
    SubscriptionPlan toPlan,
    int? daysUntilDowngrade,
    decimal priceAmount,
    decimal mrrImpact,
    string currency
)
    : TelemetryEvent(("subscription_id", subscriptionId), ("from_plan", fromPlan), ("to_plan", toPlan), ("days_until_downgrade", daysUntilDowngrade), ("price_amount", priceAmount), ("mrr_impact", mrrImpact), ("currency", currency));

public sealed class SubscriptionExpired(
    SubscriptionId subscriptionId,
    SubscriptionPlan previousPlan,
    int daysOnCurrentPlan,
    decimal previousPriceAmount,
    decimal mrrImpact,
    string previousCurrency
)
    : TelemetryEvent(("subscription_id", subscriptionId), ("previous_plan", previousPlan), ("days_on_current_plan", daysOnCurrentPlan), ("previous_price_amount", previousPriceAmount), ("mrr_impact", mrrImpact), ("previous_currency", previousCurrency));

public sealed class SubscriptionReactivated(
    SubscriptionId subscriptionId,
    SubscriptionPlan plan,
    int? daysUntilExpiry,
    int daysSinceCancelled,
    decimal priceAmount,
    decimal mrrImpact,
    string currency
)
    : TelemetryEvent(("subscription_id", subscriptionId), ("plan", plan), ("days_until_expiry", daysUntilExpiry), ("days_since_cancelled", daysSinceCancelled), ("price_amount", priceAmount), ("mrr_impact", mrrImpact), ("currency", currency));

public sealed class SubscriptionRenewed(SubscriptionId subscriptionId, SubscriptionPlan plan, decimal priceAmount, decimal mrrImpact, string currency)
    : TelemetryEvent(("subscription_id", subscriptionId), ("plan", plan), ("price_amount", priceAmount), ("mrr_impact", mrrImpact), ("currency", currency));

public sealed class SubscriptionSuspended(
    SubscriptionId subscriptionId,
    SubscriptionPlan plan,
    SuspensionReason suspensionReason,
    decimal priceAmount,
    decimal mrrImpact,
    string currency
)
    : TelemetryEvent(("subscription_id", subscriptionId), ("plan", plan), ("suspension_reason", suspensionReason), ("price_amount", priceAmount), ("mrr_impact", mrrImpact), ("currency", currency));

public sealed class SubscriptionUpgraded(
    SubscriptionId subscriptionId,
    SubscriptionPlan fromPlan,
    SubscriptionPlan toPlan,
    int daysOnCurrentPlan,
    decimal previousPriceAmount,
    decimal newPriceAmount,
    decimal mrrImpact,
    string currency
)
    : TelemetryEvent(("subscription_id", subscriptionId), ("from_plan", fromPlan), ("to_plan", toPlan), ("days_on_current_plan", daysOnCurrentPlan), ("previous_price_amount", previousPriceAmount), ("new_price_amount", newPriceAmount), ("mrr_impact", mrrImpact), ("currency", currency));

public sealed class TenantCreated(TenantId tenantId, TenantState state)
    : TelemetryEvent(("tenant_id", tenantId), ("tenant_state", state));

public sealed class TenantDeleted(TenantId tenantId, TenantState tenantState, int usersDeleted)
    : TelemetryEvent(("tenant_id", tenantId), ("tenant_state", tenantState), ("users_deleted", usersDeleted));

public sealed class TenantLogoRemoved
    : TelemetryEvent;

public sealed class TenantLogoUpdated(string contentType, long size)
    : TelemetryEvent(("content_type", contentType), ("size", size));

public sealed class TenantSwitched(TenantId fromTenantId, TenantId toTenantId, UserId userId)
    : TelemetryEvent(("from_tenant_id", fromTenantId), ("to_tenant_id", toTenantId), ("user_id", userId));

public sealed class TenantUpdated
    : TelemetryEvent;

public sealed class UserAvatarRemoved
    : TelemetryEvent;

public sealed class UserAvatarUpdated(string contentType, long size)
    : TelemetryEvent(("content_type", contentType), ("size", size));

public sealed class UserCreated(UserId userId, bool gravatarProfileFound)
    : TelemetryEvent(("user_id", userId), ("gravatar_profile_found", gravatarProfileFound));

public sealed class UserDeleted(UserId userId, bool bulkDeletion = false)
    : TelemetryEvent(("user_id", userId), ("bulk_deletion", bulkDeletion));

public sealed class UserInviteAccepted(UserId userId, int inviteAcceptedTimeInMinutes)
    : TelemetryEvent(("user_id", userId), ("invite_accepted_time_in_minutes", inviteAcceptedTimeInMinutes));

public sealed class UserInviteDeclined(UserId userId, int inviteExistedTimeInMinutes)
    : TelemetryEvent(("user_id", userId), ("invite_existed_time_in_minutes", inviteExistedTimeInMinutes));

public sealed class UserInvited(UserId userId)
    : TelemetryEvent(("user_id", userId));

public sealed class UserLocaleChanged(string fromLocale, string toLocale)
    : TelemetryEvent(("from_locale", fromLocale), ("to_locale", toLocale));

public sealed class UserPurged(UserId userId, UserPurgeReason reason)
    : TelemetryEvent(("user_id", userId), ("reason", reason));

public sealed class UserRestored(UserId userId)
    : TelemetryEvent(("user_id", userId));

public sealed class UserRoleChanged(UserId userId, UserRole fromRole, UserRole toRole)
    : TelemetryEvent(("user_id", userId), ("from_role", fromRole), ("to_role", toRole));

public sealed class UserThemeChanged(string fromTheme, string toTheme, string? resolvedTheme)
    : TelemetryEvent(("from_theme", fromTheme), ("to_theme", toTheme), ("resolved_theme", resolvedTheme as object ?? "none"));

public sealed class UserUpdated
    : TelemetryEvent;

public sealed class UserZoomLevelChanged(string fromZoomLevel, string toZoomLevel)
    : TelemetryEvent(("from_zoom_level", fromZoomLevel), ("to_zoom_level", toZoomLevel));

public sealed class UsersBulkDeleted(int count)
    : TelemetryEvent(("count", count));
