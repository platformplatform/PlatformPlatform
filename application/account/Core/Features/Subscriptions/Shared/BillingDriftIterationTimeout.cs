namespace Account.Features.Subscriptions.Shared;

/// <summary>
///     Per-iteration timeout budget for <c>BillingDriftWorker</c>. The worker scans subscriptions one at a
///     time and acquires a row-level <c>FOR UPDATE</c> lock for each one; a slow Stripe call must not hold
///     that lock long enough to block the webhook hot path or other reconcile callers. 30s is well under
///     the app-level 45s resilience timeout, so the worker releases the lock before any other caller would
///     also time out waiting on the same row.
/// </summary>
public static class BillingDriftIterationTimeout
{
    public static readonly TimeSpan Value = TimeSpan.FromSeconds(30);

    /// <summary>
    ///     Creates a <see cref="CancellationTokenSource" /> linked to <paramref name="parentToken" /> that
    ///     additionally cancels itself after <see cref="Value" />. Caller must dispose the returned source.
    /// </summary>
    public static CancellationTokenSource CreateLinkedTokenSource(CancellationToken parentToken)
    {
        var iterationCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(parentToken);
        iterationCancellationTokenSource.CancelAfter(Value);
        return iterationCancellationTokenSource;
    }
}
