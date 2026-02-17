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
    SyncSubscriptionFromStripe syncSubscriptionFromStripe,
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

        var previousStripeSubscriptionId = subscription.StripeSubscriptionId;
        var previousFirstPaymentFailedAt = subscription.FirstPaymentFailedAt;
        var previousCancellationReason = subscription.CancellationReason;
        var previousPlan = subscription.Plan;

        var syncResult = await syncSubscriptionFromStripe.ExecuteAsync(subscription, cancellationToken);

        if (syncResult.IsCustomerDeleted)
        {
            var tenant = (await tenantRepository.GetByIdUnfilteredAsync(subscription.TenantId, cancellationToken))!;
            tenant.Suspend(SuspensionReason.CustomerDeleted, now);
            tenantRepository.Update(tenant);
        }
        else
        {
            if (previousStripeSubscriptionId is null && subscription.StripeSubscriptionId is not null)
            {
                var tenant = (await tenantRepository.GetByIdUnfilteredAsync(subscription.TenantId, cancellationToken))!;
                if (tenant.State != TenantState.Active)
                {
                    tenant.Activate();
                    tenantRepository.Update(tenant);
                }

                events.CollectEvent(new SubscriptionCreated(subscription.Id, subscription.Plan));
            }

            if (previousStripeSubscriptionId is not null && subscription.StripeSubscriptionId is null)
            {
                var tenant = (await tenantRepository.GetByIdUnfilteredAsync(subscription.TenantId, cancellationToken))!;
                if (tenant.State != TenantState.Suspended)
                {
                    if (previousCancellationReason is not null && previousFirstPaymentFailedAt is null)
                    {
                        tenant.Activate();
                    }
                    else
                    {
                        tenant.Suspend(SuspensionReason.PaymentFailed, now);
                        events.CollectEvent(new SubscriptionSuspended(subscription.Id, previousPlan));
                    }

                    tenantRepository.Update(tenant);
                }
            }

            if (syncResult.SubscriptionStatus == StripeSubscriptionStatus.PastDue && previousFirstPaymentFailedAt is null)
            {
                subscription.SetPaymentFailed(now);
                events.CollectEvent(new PaymentFailed(subscription.Id, subscription.Plan));
            }

            if (syncResult.SubscriptionStatus == StripeSubscriptionStatus.Active && previousFirstPaymentFailedAt is not null)
            {
                subscription.ClearPaymentFailure();
                events.CollectEvent(new PaymentRecovered(subscription.Id, subscription.Plan));
            }
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
}
