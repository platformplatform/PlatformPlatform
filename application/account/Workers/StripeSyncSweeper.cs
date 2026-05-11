using Account.Features.Subscriptions.Domain;
using Account.Features.Subscriptions.Shared;

namespace Account.Workers;

/// <summary>
///     Periodically forces a Stripe sync for every active paid subscription so the events.list-driven
///     hot path always has a fresh anchor inside Stripe's 30-day events.list retention window. Without
///     this, a customer that happens to receive no Stripe webhooks for 30+ days would silently fall
///     outside the window and miss subsequent events. The sweeper drives
///     <see cref="ProcessPendingStripeEvents" /> per customer; the per-customer pessimistic lock inside
///     that flow serializes with concurrent webhook processing. Sweep interval defaults to 29 days
///     (just under Stripe's 30-day retention so a single missed run never escapes the window).
/// </summary>
public sealed class StripeSyncSweeper(IServiceProvider serviceProvider, IConfiguration configuration, ILogger<StripeSyncSweeper> logger) : BackgroundService
{
    private static readonly TimeSpan DefaultInterval = TimeSpan.FromDays(29);
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = configuration.GetValue<TimeSpan?>("StripeSync:SweeperInterval") ?? DefaultInterval;
        if (interval <= TimeSpan.Zero)
        {
            logger.LogInformation("Stripe sync sweeper disabled (configured interval '{Interval}' is not positive)", interval);
            return;
        }

        logger.LogInformation("Stripe sync sweeper enabled, interval '{Interval}'", interval);

        // Give the host time to settle before the first run so a deploy that restarts every replica
        // doesn't immediately hammer Stripe.
        try
        {
            await Task.Delay(StartupDelay, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        using var timer = new PeriodicTimer(interval);
        do
        {
            try
            {
                await RunSweepAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                // Don't kill the timer on a single failure — the next tick will retry.
                logger.LogError(ex, "Stripe sync sweeper run failed");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunSweepAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var subscriptionRepository = scope.ServiceProvider.GetRequiredService<ISubscriptionRepository>();
        var processor = scope.ServiceProvider.GetRequiredService<ProcessPendingStripeEvents>();

        var activeSubscriptions = await subscriptionRepository.GetAllActiveUnfilteredAsync(cancellationToken);
        logger.LogInformation("Stripe sync sweeper starting run over {Count} active subscriptions", activeSubscriptions.Length);

        var processed = 0;
        var skipped = 0;
        foreach (var subscription in activeSubscriptions)
        {
            if (cancellationToken.IsCancellationRequested) break;
            if (subscription.StripeCustomerId is null)
            {
                skipped++;
                continue;
            }

            try
            {
                // Each customer gets its own scope-of-execution; ProcessPendingStripeEvents holds the
                // per-customer pessimistic lock during its transaction so concurrent webhook delivery
                // for the same customer serializes correctly with the sweeper.
                await processor.ExecuteAsync(subscription.StripeCustomerId, true, cancellationToken);
                processed++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Stripe sync sweeper failed for customer '{StripeCustomerId}'", subscription.StripeCustomerId);
            }
        }

        logger.LogInformation("Stripe sync sweeper completed: processed {Processed}, skipped {Skipped}", processed, skipped);
    }
}
