using System.Data;
using Microsoft.ApplicationInsights;
using Microsoft.EntityFrameworkCore;
using PlatformPlatform.Account.Database;
using PlatformPlatform.Account.Features.Subscriptions.Domain;
using PlatformPlatform.Account.Features.Tenants.Domain;
using PlatformPlatform.Account.Integrations.Stripe;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.Account.Features.Subscriptions.Shared;

/// <summary>
///     Phase 2 of two-phase webhook processing. Acquires a pessimistic lock on the subscription row
///     to serialize concurrent webhook processing, syncs current state from Stripe, then applies
///     side effects (tenant state changes) based on state diffs between local and synced data.
/// </summary>
public sealed class ProcessPendingStripeEvents(
    AccountDbContext dbContext,
    ISubscriptionRepository subscriptionRepository,
    IStripeEventRepository stripeEventRepository,
    ITenantRepository tenantRepository,
    StripeClientFactory stripeClientFactory,
    TimeProvider timeProvider,
    ITelemetryEventsCollector events,
    TelemetryClient telemetryClient,
    ILogger<ProcessPendingStripeEvents> logger
)
{
    public async Task ExecuteAsync(StripeCustomerId stripeCustomerId, CancellationToken cancellationToken)
    {
        // Pessimistic lock serializes concurrent webhook processing for the same customer
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

        var tenant = (await tenantRepository.GetByIdUnfilteredAsync(subscription.TenantId, cancellationToken))!;
        var pendingEvents = await stripeEventRepository.GetPendingByStripeCustomerIdAsync(stripeCustomerId, cancellationToken);

        if (pendingEvents.Length > 0)
        {
            await SyncStateFromStripe(tenant, subscription, cancellationToken);

            MarkAllEventsAsProcessed(pendingEvents, subscription);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        SendTelemetryEvents(tenant, subscription);
    }

    private async Task SyncStateFromStripe(Tenant tenant, Subscription subscription, CancellationToken cancellationToken)
    {
        // Fetch current state from Stripe
        var stripeClient = stripeClientFactory.GetClient();
        var customerResult = await stripeClient.GetCustomerBillingInfoAsync(subscription.StripeCustomerId!, cancellationToken);

        var previousPlan = subscription.Plan;
        var previousPriceAmount = subscription.CurrentPriceAmount;
        var previousPriceCurrency = subscription.CurrentPriceCurrency;

        if (customerResult is null)
        {
            logger.LogError("Failed to fetch billing info for Stripe customer '{StripeCustomerId}'", subscription.StripeCustomerId);
            return;
        }

        if (customerResult.IsCustomerDeleted)
        {
            subscription.ResetToFreePlan();
            tenant.Suspend(SuspensionReason.CustomerDeleted, timeProvider.GetUtcNow());
            tenantRepository.Update(tenant);
            subscriptionRepository.Update(subscription);
            events.CollectEvent(new SubscriptionSuspended(subscription.Id, previousPlan, SuspensionReason.CustomerDeleted, previousPriceAmount!.Value, -previousPriceAmount.Value, previousPriceCurrency!));
            return;
        }

        var stripeState = await stripeClient.SyncSubscriptionStateAsync(subscription.StripeCustomerId!, cancellationToken);

        // Detect state transitions in lifecycle order (variables and if-blocks below follow the same order)
        var billingInfoAdded = subscription.BillingInfo is null && customerResult.BillingInfo is not null;
        var billingInfoUpdated = subscription.BillingInfo is not null && customerResult.BillingInfo is not null && customerResult.BillingInfo != subscription.BillingInfo;
        var latestPaymentMethod = stripeState?.PaymentMethod ?? customerResult.PaymentMethod;
        var paymentMethodUpdated = latestPaymentMethod != subscription.PaymentMethod;
        var subscriptionCreated = subscription.StripeSubscriptionId is null && stripeState?.StripeSubscriptionId is not null;
        var subscriptionRenewed = subscription.CurrentPeriodEnd is not null && stripeState?.CurrentPeriodEnd is not null && stripeState.CurrentPeriodEnd > subscription.CurrentPeriodEnd;
        var subscriptionUpgraded = !subscriptionCreated && stripeState is not null && stripeState.Plan != subscription.Plan && stripeState.Plan.IsUpgradeFrom(subscription.Plan);
        var downgradeScheduled = subscription.ScheduledPlan is null && stripeState?.ScheduledPlan is not null;
        var downgradeCancelled = subscription.ScheduledPlan is not null && stripeState?.ScheduledPlan is null && stripeState is not null && stripeState.Plan == subscription.Plan;
        var subscriptionDowngraded = subscription.ScheduledPlan is not null && stripeState?.ScheduledPlan is null && stripeState is not null && stripeState.Plan != subscription.Plan && subscription.Plan.IsUpgradeFrom(stripeState.Plan);
        var subscriptionCancelled = !subscription.CancelAtPeriodEnd && stripeState?.CancelAtPeriodEnd == true;
        var subscriptionReactivated = subscription.CancelAtPeriodEnd && stripeState?.CancelAtPeriodEnd == false;
        var subscriptionExpired = subscription.StripeSubscriptionId is not null && stripeState is null && subscription is { CancelAtPeriodEnd: true, FirstPaymentFailedAt: null };
        var subscriptionImmediatelyCancelled = subscription.StripeSubscriptionId is not null && stripeState is null && subscription is { CancelAtPeriodEnd: false, FirstPaymentFailedAt: null };
        var subscriptionSuspended = subscription.StripeSubscriptionId is not null && stripeState is null && subscription.FirstPaymentFailedAt is not null;
        var paymentFailed = stripeState?.SubscriptionStatus is StripeSubscriptionStatus.PastDue or StripeSubscriptionStatus.Incomplete && subscription.FirstPaymentFailedAt is null;
        var paymentRecovered = stripeState?.SubscriptionStatus == StripeSubscriptionStatus.Active && subscription.FirstPaymentFailedAt is not null;
        var previousRefundCount = subscription.PaymentTransactions.Count(t => t.Status == PaymentTransactionStatus.Refunded);
        var now = timeProvider.GetUtcNow();
        var daysOnCurrentPlan = (int)(now - (subscription.ModifiedAt ?? subscription.CreatedAt)).TotalDays;

        // Apply Stripe state to aggregate (after detection, before side effects)
        if (stripeState is not null)
        {
            subscription.SetStripeSubscription(stripeState.StripeSubscriptionId, stripeState.Plan, stripeState.CurrentPriceAmount, stripeState.CurrentPriceCurrency, stripeState.CurrentPeriodEnd, stripeState.PaymentMethod);
        }

        // Always sync payment transactions from Stripe (via subscription when active, via invoices when cancelled)
        var syncedTransactions = stripeState?.PaymentTransactions ?? await stripeClient.SyncPaymentTransactionsAsync(subscription.StripeCustomerId!, cancellationToken);
        if (syncedTransactions is not null)
        {
            subscription.SetPaymentTransactions([.. syncedTransactions]);
        }

        var paymentRefunded = subscription.PaymentTransactions.Count(t => t.Status == PaymentTransactionStatus.Refunded) > previousRefundCount;

        if (billingInfoAdded)
        {
            subscription.SetBillingInfo(customerResult.BillingInfo);
            events.CollectEvent(new BillingInfoAdded(subscription.Id, customerResult.BillingInfo?.Address?.Country, customerResult.BillingInfo?.Address?.PostalCode, customerResult.BillingInfo?.Address?.City));
        }

        if (billingInfoUpdated)
        {
            subscription.SetBillingInfo(customerResult.BillingInfo);
            events.CollectEvent(new BillingInfoUpdated(subscription.Id, customerResult.BillingInfo?.Address?.Country, customerResult.BillingInfo?.Address?.PostalCode, customerResult.BillingInfo?.Address?.City));
        }

        if (paymentMethodUpdated)
        {
            subscription.SetPaymentMethod(latestPaymentMethod);
            events.CollectEvent(new PaymentMethodUpdated(subscription.Id));
        }

        if (subscriptionCreated)
        {
            tenant.Activate();
            events.CollectEvent(new SubscriptionCreated(subscription.Id, subscription.Plan, subscription.CurrentPriceAmount!.Value, subscription.CurrentPriceAmount!.Value, subscription.CurrentPriceCurrency!));
        }

        if (subscriptionRenewed)
        {
            events.CollectEvent(new SubscriptionRenewed(subscription.Id, subscription.Plan, subscription.CurrentPriceAmount!.Value, subscription.CurrentPriceAmount!.Value - previousPriceAmount!.Value, subscription.CurrentPriceCurrency!));
        }

        if (subscriptionUpgraded)
        {
            events.CollectEvent(new SubscriptionUpgraded(subscription.Id, previousPlan, subscription.Plan, daysOnCurrentPlan, previousPriceAmount!.Value, subscription.CurrentPriceAmount!.Value, subscription.CurrentPriceAmount!.Value - previousPriceAmount.Value, subscription.CurrentPriceCurrency!));
        }

        if (downgradeScheduled)
        {
            subscription.SetScheduledPlan(stripeState!.ScheduledPlan);
            var daysUntilDowngrade = subscription.CurrentPeriodEnd is not null ? (int)(subscription.CurrentPeriodEnd.Value - now).TotalDays : (int?)null;
            var priceCatalog = await stripeClient.GetPriceCatalogAsync(cancellationToken);
            var scheduledPlanPrice = priceCatalog.Single(p => p.Plan == subscription.ScheduledPlan!.Value).UnitAmount;
            events.CollectEvent(new SubscriptionDowngradeScheduled(subscription.Id, subscription.Plan, subscription.ScheduledPlan!.Value, daysUntilDowngrade, subscription.CurrentPriceAmount!.Value, scheduledPlanPrice - subscription.CurrentPriceAmount!.Value, subscription.CurrentPriceCurrency!));
        }

        if (downgradeCancelled)
        {
            var previousScheduledPlan = subscription.ScheduledPlan;
            var daysSinceDowngradeScheduled = (int)(now - (subscription.ModifiedAt ?? subscription.CreatedAt)).TotalDays;
            subscription.SetScheduledPlan(stripeState!.ScheduledPlan);
            var daysUntilDowngrade = subscription.CurrentPeriodEnd is not null ? (int)(subscription.CurrentPeriodEnd.Value - now).TotalDays : (int?)null;
            var priceCatalog = await stripeClient.GetPriceCatalogAsync(cancellationToken);
            var scheduledPlanPrice = priceCatalog.Single(p => p.Plan == previousScheduledPlan!.Value).UnitAmount;
            events.CollectEvent(new SubscriptionDowngradeCancelled(subscription.Id, subscription.Plan, previousScheduledPlan!.Value, daysUntilDowngrade, daysSinceDowngradeScheduled, subscription.CurrentPriceAmount!.Value, subscription.CurrentPriceAmount!.Value - scheduledPlanPrice, subscription.CurrentPriceCurrency!));
        }

        if (subscriptionDowngraded)
        {
            subscription.SetScheduledPlan(stripeState!.ScheduledPlan);
            events.CollectEvent(new SubscriptionDowngraded(subscription.Id, previousPlan, subscription.Plan, daysOnCurrentPlan, previousPriceAmount!.Value, subscription.CurrentPriceAmount!.Value, subscription.CurrentPriceAmount!.Value - previousPriceAmount.Value, subscription.CurrentPriceCurrency!));
        }

        if (subscriptionCancelled)
        {
            subscription.SetCancellation(stripeState!.CancelAtPeriodEnd, stripeState.CancellationReason, stripeState.CancellationFeedback);
            var daysUntilExpiry = subscription.CurrentPeriodEnd is not null ? (int)(subscription.CurrentPeriodEnd.Value - now).TotalDays : (int?)null;
            events.CollectEvent(new SubscriptionCancelled(subscription.Id, subscription.Plan, subscription.CancellationReason ?? CancellationReason.CancelledByAdmin, daysUntilExpiry, daysOnCurrentPlan, subscription.CurrentPriceAmount!.Value, -subscription.CurrentPriceAmount!.Value, subscription.CurrentPriceCurrency!));
        }

        if (subscriptionReactivated)
        {
            var daysSinceCancelled = (int)(now - (subscription.ModifiedAt ?? subscription.CreatedAt)).TotalDays;
            subscription.SetCancellation(stripeState!.CancelAtPeriodEnd, stripeState.CancellationReason, stripeState.CancellationFeedback);
            var daysUntilExpiry = subscription.CurrentPeriodEnd is not null ? (int)(subscription.CurrentPeriodEnd.Value - now).TotalDays : (int?)null;
            events.CollectEvent(new SubscriptionReactivated(subscription.Id, subscription.Plan, daysUntilExpiry, daysSinceCancelled, subscription.CurrentPriceAmount!.Value, subscription.CurrentPriceAmount!.Value, subscription.CurrentPriceCurrency!));
        }

        if (subscriptionExpired)
        {
            subscription.ResetToFreePlan();
            events.CollectEvent(new SubscriptionExpired(subscription.Id, previousPlan, daysOnCurrentPlan, previousPriceAmount!.Value, -previousPriceAmount.Value, previousPriceCurrency!));
        }

        if (subscriptionImmediatelyCancelled)
        {
            subscription.ResetToFreePlan();
            events.CollectEvent(new SubscriptionCancelled(subscription.Id, previousPlan, CancellationReason.CancelledByAdmin, 0, daysOnCurrentPlan, previousPriceAmount!.Value, -previousPriceAmount.Value, previousPriceCurrency!));
        }

        if (subscriptionSuspended)
        {
            subscription.ResetToFreePlan();
            tenant.Suspend(SuspensionReason.PaymentFailed, timeProvider.GetUtcNow());
            events.CollectEvent(new SubscriptionSuspended(subscription.Id, previousPlan, SuspensionReason.PaymentFailed, previousPriceAmount!.Value, -previousPriceAmount.Value, previousPriceCurrency!));
        }

        if (paymentFailed)
        {
            subscription.SetPaymentFailed(now);
            events.CollectEvent(new PaymentFailed(subscription.Id, subscription.Plan, subscription.CurrentPriceAmount!.Value, subscription.CurrentPriceCurrency!));
        }

        if (paymentRecovered)
        {
            var daysInPastDue = (int)(now - subscription.FirstPaymentFailedAt!.Value).TotalDays;
            subscription.ClearPaymentFailure();
            events.CollectEvent(new PaymentRecovered(subscription.Id, subscription.Plan, daysInPastDue, subscription.CurrentPriceAmount!.Value, subscription.CurrentPriceCurrency!));
        }

        if (paymentRefunded)
        {
            var refundedTransactions = subscription.PaymentTransactions.Where(t => t.Status == PaymentTransactionStatus.Refunded).ToArray();
            var refundCount = refundedTransactions.Length - previousRefundCount;
            var latestRefund = refundedTransactions[^1];
            var plan = stripeState is not null ? subscription.Plan : previousPlan;
            events.CollectEvent(new PaymentRefunded(subscription.Id, plan, refundCount, latestRefund.Amount, latestRefund.Currency));
        }

        // Persist all aggregate mutations and mark pending events as processed
        if (subscriptionCreated || subscriptionSuspended)
        {
            tenantRepository.Update(tenant);
        }

        subscriptionRepository.Update(subscription);
    }

    private void MarkAllEventsAsProcessed(StripeEvent[] pendingEvents, Subscription subscription)
    {
        var now = timeProvider.GetUtcNow();

        foreach (var pendingEvent in pendingEvents)
        {
            pendingEvent.MarkProcessed(now);
            pendingEvent.SetStripeSubscriptionId(subscription.StripeSubscriptionId);
            pendingEvent.SetTenantId(subscription.TenantId);
            stripeEventRepository.Update(pendingEvent);
        }
    }

    private void SendTelemetryEvents(Tenant tenant, Subscription subscription)
    {
        TenantScopedTelemetryContext.Set(tenant.Id, subscription.Plan.ToString());

        // Publish collected telemetry events after successful commit
        while (events.HasEvents)
        {
            var telemetryEvent = events.Dequeue();
            telemetryClient.TrackEvent(telemetryEvent.GetType().Name, telemetryEvent.Properties);
            logger.LogInformation("Telemetry: {EventName} {EventProperties}", telemetryEvent.GetType().Name, string.Join(", ", telemetryEvent.Properties.Select(p => $"{p.Key}={p.Value}")));
        }
    }
}
