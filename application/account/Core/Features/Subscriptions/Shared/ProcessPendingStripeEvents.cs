using System.Data;
using Microsoft.ApplicationInsights;
using Microsoft.EntityFrameworkCore;
using PlatformPlatform.Account.Database;
using PlatformPlatform.Account.Features.Subscriptions.Domain;
using PlatformPlatform.Account.Features.Tenants.Domain;
using PlatformPlatform.SharedKernel.Integrations.Email;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.Account.Features.Subscriptions.Shared;

/// <summary>
///     Phase 2 of two-phase webhook processing. Acquires a pessimistic lock on the subscription row
///     to serialize concurrent webhook processing, syncs current state from Stripe, then applies
///     side effects (emails, tenant state changes) based on the batch of event types.
/// </summary>
public sealed class ProcessPendingStripeEvents(
    AccountDbContext dbContext,
    ISubscriptionRepository subscriptionRepository,
    IStripeEventRepository stripeEventRepository,
    ITenantRepository tenantRepository,
    SyncSubscriptionFromStripe syncSubscriptionFromStripe,
    IEmailClient emailClient,
    TimeProvider timeProvider,
    ITelemetryEventsCollector events,
    TelemetryClient telemetryClient,
    ILogger<ProcessPendingStripeEvents> logger
)
{
    private static readonly TimeSpan GracePeriod = TimeSpan.FromHours(72);
    private static readonly TimeSpan NotificationCooldown = TimeSpan.FromHours(24);

    public async Task ExecuteAsync(StripeCustomerId stripeCustomerId, CancellationToken cancellationToken)
    {
        var isSqlite = dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite";
        await using var transaction = isSqlite
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

        var subscription = await subscriptionRepository.GetByStripeCustomerIdWithLockUnfilteredAsync(stripeCustomerId, cancellationToken);
        if (subscription is null)
        {
            logger.LogWarning("Subscription not found for Stripe customer '{StripeCustomerId}', events will be retried on next webhook", stripeCustomerId);
            await transaction.RollbackAsync(cancellationToken);
            return;
        }

        var pendingEvents = await stripeEventRepository.GetPendingByStripeCustomerIdAsync(stripeCustomerId, cancellationToken);

        if (pendingEvents.Length == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return;
        }

        var now = timeProvider.GetUtcNow();

        await syncSubscriptionFromStripe.ExecuteAsync(subscription, cancellationToken);

        var eventTypes = pendingEvents.Select(e => e.EventType).ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (eventTypes.Contains("invoice.payment_succeeded"))
        {
            await HandlePaymentSucceeded(subscription, cancellationToken);
        }

        if (eventTypes.Contains("invoice.payment_failed"))
        {
            await HandlePaymentFailed(subscription, now, cancellationToken);
        }

        if (eventTypes.Contains("charge.dispute.created"))
        {
            await HandleDisputeCreated(subscription, now, cancellationToken);
        }

        if (eventTypes.Contains("charge.dispute.closed"))
        {
            HandleDisputeClosed(subscription);
        }

        if (eventTypes.Contains("charge.refunded"))
        {
            HandleRefund(subscription, now);
        }

        if (eventTypes.Contains("checkout.session.completed"))
        {
            events.CollectEvent(new SubscriptionCreated(subscription.Id, subscription.Plan));
        }

        if (eventTypes.Contains("customer.subscription.deleted"))
        {
            await HandleSubscriptionDeleted(subscription, cancellationToken);
        }

        subscriptionRepository.Update(subscription);

        foreach (var pendingEvent in pendingEvents)
        {
            pendingEvent.MarkProcessed(now);
            pendingEvent.SetStripeSubscriptionId(subscription.StripeSubscriptionId);
            pendingEvent.SetTenantId(subscription.TenantId);
            stripeEventRepository.Update(pendingEvent);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        while (events.HasEvents)
        {
            var telemetryEvent = events.Dequeue();
            telemetryClient.TrackEvent(telemetryEvent.GetType().Name, telemetryEvent.Properties);
        }
    }

    /// <summary>
    ///     Clears payment failure state and restores tenant to Active if currently PastDue.
    /// </summary>
    private async Task HandlePaymentSucceeded(Subscription subscription, CancellationToken cancellationToken)
    {
        if (subscription.FirstPaymentFailedAt is null) return;

        subscription.ClearPaymentFailure();

        var tenant = (await tenantRepository.GetByIdUnfilteredAsync(subscription.TenantId, cancellationToken))!;
        if (tenant.State == TenantState.PastDue)
        {
            tenant.SetState(TenantState.Active);
            tenantRepository.Update(tenant);
        }

        events.CollectEvent(new PaymentRecovered(subscription.Id, subscription.Plan));
    }

    /// <summary>
    ///     Implements a 72-hour grace period with 24-hour reminder emails. First failure sets PastDue;
    ///     after grace period expires, suspends the tenant.
    /// </summary>
    private async Task HandlePaymentFailed(Subscription subscription, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var tenant = (await tenantRepository.GetByIdUnfilteredAsync(subscription.TenantId, cancellationToken))!;
        var billingEmail = subscription.BillingInfo?.Email;

        if (subscription.FirstPaymentFailedAt is null)
        {
            subscription.SetPaymentFailed(now);
            subscription.SetLastNotificationSentAt(now);

            if (tenant.State != TenantState.Suspended)
            {
                tenant.SetState(TenantState.PastDue);
                tenantRepository.Update(tenant);
            }

            if (billingEmail is not null)
            {
                await SendPaymentFailedEmail(billingEmail, cancellationToken);
            }

            events.CollectEvent(new PaymentFailed(subscription.Id, subscription.Plan));
        }
        else
        {
            var timeSinceFirstFailure = now - subscription.FirstPaymentFailedAt.Value;

            if (timeSinceFirstFailure >= GracePeriod)
            {
                if (tenant.State != TenantState.Suspended)
                {
                    tenant.SetState(TenantState.Suspended);
                    tenantRepository.Update(tenant);
                }

                if (billingEmail is not null)
                {
                    await SendSubscriptionSuspendedEmail(billingEmail, cancellationToken);
                }

                events.CollectEvent(new SubscriptionSuspended(subscription.Id, subscription.Plan));
            }
            else
            {
                var shouldSendReminder = subscription.LastNotificationSentAt is null ||
                                         now - subscription.LastNotificationSentAt.Value >= NotificationCooldown;

                if (shouldSendReminder && billingEmail is not null)
                {
                    var hoursRemaining = (GracePeriod - timeSinceFirstFailure).TotalHours;
                    var daysRemaining = Math.Max(1, (int)Math.Ceiling(hoursRemaining / 24));
                    await SendGracePeriodReminderEmail(billingEmail, daysRemaining, cancellationToken);
                    subscription.SetLastNotificationSentAt(now);
                }
            }
        }
    }

    /// <summary>
    ///     Suspends the tenant when Stripe deletes the subscription (e.g., after max retry failures).
    /// </summary>
    private async Task HandleSubscriptionDeleted(Subscription subscription, CancellationToken cancellationToken)
    {
        var tenant = (await tenantRepository.GetByIdUnfilteredAsync(subscription.TenantId, cancellationToken))!;
        if (tenant.State != TenantState.Suspended)
        {
            tenant.SetState(TenantState.Suspended);
            tenantRepository.Update(tenant);
        }

        events.CollectEvent(new SubscriptionSuspended(subscription.Id, subscription.Plan));
    }

    private async Task SendPaymentFailedEmail(string recipientEmail, CancellationToken cancellationToken)
    {
        const string subject = "Payment failed - action required";
        const string htmlContent = """
                                   <h2>Your payment has failed</h2>
                                   <p>We were unable to process your subscription payment. Your subscription is now past due.</p>
                                   <p>Please update your payment method to avoid service interruption. You have 3 days to resolve this before your subscription is suspended.</p>
                                   <p>You can update your payment method from your subscription settings.</p>
                                   """;

        await emailClient.SendAsync(recipientEmail, subject, htmlContent, cancellationToken);
    }

    private async Task SendGracePeriodReminderEmail(string recipientEmail, int daysRemaining, CancellationToken cancellationToken)
    {
        const string subject = "Payment reminder - subscription at risk";
        var htmlContent = $"""
                           <h2>Payment still outstanding</h2>
                           <p>Your subscription payment is still failing. You have approximately {daysRemaining} day{(daysRemaining != 1 ? "s" : "")} remaining before your subscription is suspended.</p>
                           <p>Please update your payment method as soon as possible to avoid losing access.</p>
                           <p>You can update your payment method from your subscription settings.</p>
                           """;

        await emailClient.SendAsync(recipientEmail, subject, htmlContent, cancellationToken);
    }

    private async Task SendSubscriptionSuspendedEmail(string recipientEmail, CancellationToken cancellationToken)
    {
        const string subject = "Subscription suspended";
        const string htmlContent = """
                                   <h2>Your subscription has been suspended</h2>
                                   <p>Due to continued payment failure, your subscription has been suspended. All users in your organization will have limited access until the subscription is reactivated.</p>
                                   <p>To restore access, please update your payment method and reactivate your subscription from your subscription settings.</p>
                                   """;

        await emailClient.SendAsync(recipientEmail, subject, htmlContent, cancellationToken);
    }

    private async Task HandleDisputeCreated(Subscription subscription, DateTimeOffset now, CancellationToken cancellationToken)
    {
        subscription.SetDisputed(now);

        var billingEmail = subscription.BillingInfo?.Email;
        if (billingEmail is not null)
        {
            await SendDisputeCreatedEmail(billingEmail, cancellationToken);
        }

        events.CollectEvent(new PaymentDisputed(subscription.Id, subscription.Plan));
    }

    private void HandleDisputeClosed(Subscription subscription)
    {
        subscription.ClearDispute();
        events.CollectEvent(new DisputeResolved(subscription.Id, subscription.Plan));
    }

    private void HandleRefund(Subscription subscription, DateTimeOffset now)
    {
        subscription.SetRefunded(now);
        events.CollectEvent(new PaymentRefunded(subscription.Id, subscription.Plan));
    }

    private async Task SendDisputeCreatedEmail(string recipientEmail, CancellationToken cancellationToken)
    {
        const string subject = "Payment dispute - immediate action required";
        const string htmlContent = """
                                   <h2>A payment dispute has been filed</h2>
                                   <p>A payment dispute has been filed for your subscription.</p>
                                   <p>Please review the dispute in your payment settings or contact support for assistance.</p>
                                   <p>You can manage your subscription from your subscription settings.</p>
                                   """;

        await emailClient.SendAsync(recipientEmail, subject, htmlContent, cancellationToken);
    }
}
