using Account.Features.Subscriptions.Domain;
using Account.Features.Subscriptions.Shared;

namespace Account.Workers;

/// <summary>
///     On Worker startup, runs a single detect-only pass over every subscription whose drift check is stale
///     (no DriftCheckedAt yet, or older than the configured staleness threshold) and asks
///     <see cref="ProcessPendingStripeEvents" /> to flag drift via <c>SyncMode.Detect</c>. The worker is a
///     tripwire — it never mutates persisted state beyond <see cref="Subscription.SetDriftStatus" />, so any
///     real remediation stays on the webhook hot path and the back-office reconcile admin action. Azure
///     Container Apps Workers scale down to zero replicas when idle and Azure sends SIGTERM on scale-down,
///     so the <see cref="BackgroundService" /> lifecycle naturally bounds this pass to a single run per
///     scale-up; we deliberately do not use a <see cref="PeriodicTimer" /> because the in-memory timer would
///     never tick before the container exited.
/// </summary>
public sealed class BillingDriftWorker(IServiceProvider serviceProvider, IConfiguration configuration, TimeProvider timeProvider, ILogger<BillingDriftWorker> logger) : BackgroundService
{
    private static readonly TimeSpan DefaultStaleness = TimeSpan.FromHours(23);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 23h (not 24h) so two consecutive daily deploys are guaranteed to both catch every row even if the
        // second deploy lands slightly more than 24h after the first.
        var staleness = configuration.GetValue<TimeSpan?>("BillingDrift:Staleness") ?? DefaultStaleness;
        var cutoff = timeProvider.GetUtcNow() - staleness;

        using var scope = serviceProvider.CreateScope();
        var subscriptionRepository = scope.ServiceProvider.GetRequiredService<ISubscriptionRepository>();
        var processor = scope.ServiceProvider.GetRequiredService<ProcessPendingStripeEvents>();

        var dueSubscriptions = await subscriptionRepository.GetSubscriptionsDueForDriftCheckUnfilteredAsync(cutoff, stoppingToken);
        logger.LogInformation("Billing drift worker starting detect pass over {Count} subscriptions (staleness '{Staleness}')", dueSubscriptions.Length, staleness);

        var processed = 0;
        var skipped = 0;
        var failed = 0;
        foreach (var subscription in dueSubscriptions)
        {
            if (stoppingToken.IsCancellationRequested) break;
            if (subscription.StripeCustomerId is null)
            {
                skipped++;
                continue;
            }

            try
            {
                await processor.ExecuteAsync(subscription.StripeCustomerId, true, SyncMode.Detect, stoppingToken);
                processed++;
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                // Per-subscription failure must not kill the whole pass; one bad customer cannot block drift
                // detection for everyone else.
                failed++;
                logger.LogError(ex, "Billing drift worker failed for customer '{StripeCustomerId}'", subscription.StripeCustomerId);
            }
        }

        logger.LogInformation("Billing drift worker completed: processed {Processed}, skipped {Skipped}, failed {Failed}", processed, skipped, failed);
    }
}
