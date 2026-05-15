using System.Security.Cryptography;
using System.Text;
using Account.Database;
using Account.Features;
using Account.Features.FeatureFlags.Domain;
using Microsoft.ApplicationInsights;
using Microsoft.EntityFrameworkCore;
using SharedKernel.FeatureFlags;
using SharedKernel.Telemetry;

namespace Account.Workers;

/// <summary>
///     Converges the <c>feature_flags</c> table to the C# definitions in
///     <see cref="SharedKernel.FeatureFlags.FeatureFlags" /> on every worker startup. Replaces the
///     deleted one-shot <c>SeedFeatureFlags</c> and <c>SeedPlanBasedFeatureFlags</c> data migrations
///     with a converging path that handles future flag-definition changes (plan-tier transitions,
///     flag removal) without writing new data migrations.
///     For every non-System definition the reconciler ensures the global row exists with the correct
///     <see cref="FeatureFlag.Source" />. The base row's IsActive state is set on creation (active
///     when <see cref="FeatureFlagDefinition.IsKillSwitchEnabled" /> is <c>false</c>, inactive
///     otherwise) and thereafter is owned by admins via Activate/Deactivate commands — the
///     reconciler never flips it. Stale tenant overrides whose Source no longer matches the
///     definition are removed (so
///     <see cref="Features.FeatureFlags.Shared.PlanBasedFeatureFlagEvaluator" /> can rebuild them on
///     the next login). Any DB row whose flag_key is not in the C# definitions is marked
///     <see cref="FeatureFlag.OrphanedAt" /> at the current time. If a definition reuses a key that
///     was previously soft-deleted, the reconciler throws to abort deployment.
///     The reconciler is idempotent — a second pass on top of a converged DB produces no changes.
///     Runs inline at Worker startup (NOT a BackgroundService) so that a failure aborts the process
///     before it accepts traffic — better to fail to start than to run with inconsistent flag state.
///     A PostgreSQL session-scoped advisory lock serializes concurrent reconciles across worker
///     replicas during rolling deploys or KEDA scale-from-zero.
/// </summary>
public sealed class FeatureFlagDefinitionReconciler(
    IFeatureFlagRepository featureFlagRepository,
    AccountDbContext accountDbContext,
    TimeProvider timeProvider,
    ITelemetryEventsCollector telemetryEventsCollector,
    TelemetryClient telemetryClient,
    ILogger<FeatureFlagDefinitionReconciler> logger
)
{
    // Stable per-reconciler advisory-lock key derived from the type name (mirrors DataMigrationRunner's
    // BitConverter.ToInt64(SHA256(typeof(TContext).FullName)) pattern). Naming the key from the type
    // namespace isolates it from the migration runner's lock so the two coexist on the same DB session.
    private static readonly long AdvisoryLockKey = BitConverter.ToInt64(
        SHA256.HashData(Encoding.UTF8.GetBytes(typeof(FeatureFlagDefinitionReconciler).FullName!))
    );

    public async Task ReconcileAsync(CancellationToken cancellationToken)
    {
        await accountDbContext.Database.ExecuteSqlAsync(
            $"SELECT pg_advisory_lock({AdvisoryLockKey})",
            cancellationToken
        );

        try
        {
            await ReconcileInternalAsync(cancellationToken);
        }
        finally
        {
            await accountDbContext.Database.ExecuteSqlAsync(
                $"SELECT pg_advisory_unlock({AdvisoryLockKey})",
                CancellationToken.None
            );
        }
    }

    private async Task ReconcileInternalAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var definitions = FeatureFlags.GetAll().Where(d => d.Scope != FeatureFlagScope.System).ToArray();

        // Load every base row up front so the per-definition reconcile loop is in-memory only. Avoids one
        // round-trip per definition; the base-row count is bounded by the C# definitions, but a single
        // query is still cheaper and matches how the rest of the codebase batches startup reads.
        var baseRowsByKey = (await featureFlagRepository.GetAllBaseRowsAsync(cancellationToken)).ToDictionary(r => r.FlagKey);

        var baseRowsCreated = 0;
        var baseRowsRestored = 0;
        var sourceTransitions = 0;
        var staleRowsRemoved = 0;

        foreach (var definition in definitions)
        {
            var expectedSource = definition.RequiredPlan is not null ? FeatureFlagSource.Plan : FeatureFlagSource.Manual;
            baseRowsByKey.TryGetValue(definition.Key, out var baseRow);

            if (baseRow is null)
            {
                baseRow = FeatureFlag.Create(definition.Key, definition.Scope, expectedSource);
                if (!definition.IsKillSwitchEnabled) baseRow.Activate(now);
                await featureFlagRepository.AddAsync(baseRow, cancellationToken);
                baseRowsCreated++;
                logger.LogInformation(
                    "Reconciler created base row for '{FlagKey}' (source '{Source}', scope '{Scope}', active '{IsActive}')",
                    definition.Key, expectedSource, definition.Scope, baseRow.IsActive
                );
                continue;
            }

            // A deleted base row with the same key signals that an admin explicitly retired the flag.
            // Reusing the key would conflate historical telemetry between the retired flag and a new one
            // that happens to share the name, so deployment is aborted. Choose a different key, or
            // manually clear DeletedAt in the database if you are sure you want to reuse it.
            if (baseRow.DeletedAt is not null)
            {
                throw new InvalidOperationException(
                    $"Feature flag '{definition.Key}' was previously deleted on {baseRow.DeletedAt:O}. Adding a new feature flag with the same name is not allowed."
                );
            }

            // An orphaned (but not deleted) base row is back in code. This is the rollback / intentional
            // reintroduction path — clear the timestamp so the row participates as a live flag again,
            // preserving historical telemetry on the same row instead of starting a new one.
            if (baseRow.OrphanedAt is not null)
            {
                baseRow.Restore();
                featureFlagRepository.Update(baseRow);
                baseRowsRestored++;
                telemetryEventsCollector.CollectEvent(new FeatureFlagRestoredByReconciler(definition.Key));
                logger.LogInformation("Reconciler restored '{FlagKey}' after re-add to the C# definitions", definition.Key);
            }

            // Converge if a definition changed scope (e.g., subtype renamed across scopes between deploys).
            if (baseRow.Scope != definition.Scope)
            {
                baseRow.SetScope(definition.Scope);
                featureFlagRepository.Update(baseRow);
            }

            if (baseRow.Source != expectedSource)
            {
                var fromSource = baseRow.Source;
                var staleTenantRows = await featureFlagRepository.GetTenantOverridesForFlagAsync(definition.Key, cancellationToken);
                var staleTenantRowsToRemove = staleTenantRows.Where(r => r.Source != expectedSource).ToArray();
                foreach (var staleRow in staleTenantRowsToRemove)
                {
                    featureFlagRepository.Remove(staleRow);
                    staleRowsRemoved++;
                }

                baseRow.SetSource(expectedSource);
                featureFlagRepository.Update(baseRow);
                sourceTransitions++;
                telemetryEventsCollector.CollectEvent(new FeatureFlagSourceTransitionedByReconciler(
                        definition.Key, fromSource, expectedSource, staleTenantRowsToRemove.Length
                    )
                );
                logger.LogInformation(
                    "Reconciler transitioned '{FlagKey}' source to '{Source}' and removed '{StaleCount}' stale tenant overrides",
                    definition.Key, expectedSource, staleTenantRowsToRemove.Length
                );
            }
        }

        var knownKeys = definitions.Select(d => d.Key).ToHashSet();
        var allRows = await featureFlagRepository.GetAllRowsUnfilteredAsync(cancellationToken);

        var orphansMarked = 0;
        foreach (var row in allRows.Where(r => !knownKeys.Contains(r.FlagKey) && r.OrphanedAt is null))
        {
            row.MarkOrphaned(now);
            featureFlagRepository.Update(row);
            orphansMarked++;
            // Only emit the structured event once per orphaned flag key (base row), not for every
            // override row that happens to carry the same stale key.
            if (row.TenantId is null && row.UserId is null)
            {
                telemetryEventsCollector.CollectEvent(new FeatureFlagOrphanedByReconciler(row.FlagKey));
            }

            logger.LogInformation("Reconciler marked '{FlagKey}' (id '{Id}') as orphaned", row.FlagKey, row.Id);
        }

        await accountDbContext.SaveChangesAsync(cancellationToken);

        // Publish AFTER the DB commit so we never emit telemetry for state that did not persist.
        // The reconciler is idempotent — a crash between SaveChanges and publish means the next
        // worker startup converges to the same state but does not re-emit (no work to do).
        while (telemetryEventsCollector.HasEvents)
        {
            var telemetryEvent = telemetryEventsCollector.Dequeue();
            telemetryClient.TrackEvent(telemetryEvent.GetType().Name, telemetryEvent.Properties);
        }

        logger.LogInformation(
            "Reconciler completed: {DefinitionCount} definitions reconciled, {BaseRowsCreated} base rows created, {BaseRowsRestored} re-added rows restored, {SourceTransitions} source transitions, {StaleRowsRemoved} stale tenant rows removed, {OrphansMarked} orphans marked",
            definitions.Length, baseRowsCreated, baseRowsRestored, sourceTransitions, staleRowsRemoved, orphansMarked
        );
    }
}
