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
            HandlePaymentSucceeded(subscription);
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
            var tenant = (await tenantRepository.GetByIdUnfilteredAsync(subscription.TenantId, cancellationToken))!;
            if (tenant.State != TenantState.Active)
            {
                tenant.Activate();
                tenantRepository.Update(tenant);
            }

            events.CollectEvent(new SubscriptionCreated(subscription.Id, subscription.Plan));
        }

        var customerDeleted = eventTypes.Contains("customer.deleted");

        if (customerDeleted)
        {
            var tenant = (await tenantRepository.GetByIdUnfilteredAsync(subscription.TenantId, cancellationToken))!;
            tenant.Suspend(SuspensionReason.CustomerDeleted, now);
            tenantRepository.Update(tenant);
        }

        if (eventTypes.Contains("customer.subscription.deleted") && !customerDeleted)
        {
            await HandleSubscriptionDeleted(subscription, now, cancellationToken);
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

    private void HandlePaymentSucceeded(Subscription subscription)
    {
        if (subscription.FirstPaymentFailedAt is null) return;

        subscription.ClearPaymentFailure();
        events.CollectEvent(new PaymentRecovered(subscription.Id, subscription.Plan));
    }

    private async Task HandlePaymentFailed(Subscription subscription, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (subscription.FirstPaymentFailedAt is not null) return;

        subscription.SetPaymentFailed(now);

        var billingEmail = subscription.BillingInfo?.Email;
        if (billingEmail is not null)
        {
            await SendPaymentFailedEmail(billingEmail, cancellationToken);
        }

        events.CollectEvent(new PaymentFailed(subscription.Id, subscription.Plan));
    }

    private async Task HandleSubscriptionDeleted(Subscription subscription, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var tenant = (await tenantRepository.GetByIdUnfilteredAsync(subscription.TenantId, cancellationToken))!;

        if (tenant.State == TenantState.Suspended) return;

        if (subscription.CancellationReason is not null && subscription.FirstPaymentFailedAt is null)
        {
            tenant.Activate();
        }
        else
        {
            tenant.Suspend(SuspensionReason.PaymentFailed, now);
            events.CollectEvent(new SubscriptionSuspended(subscription.Id, subscription.Plan));
        }

        tenantRepository.Update(tenant);
    }

    private async Task SendPaymentFailedEmail(string recipientEmail, CancellationToken cancellationToken)
    {
        const string subject = "Payment failed - action required";
        const string htmlContent = """
                                   <h2>Your payment has failed</h2>
                                   <p>We were unable to process your subscription payment.</p>
                                   <p>Please update your payment method to avoid service interruption.</p>
                                   <p>You can update your payment method from your subscription settings.</p>
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
