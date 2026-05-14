using Account.Database;
using Account.Features.FeatureFlags.Domain;
using SharedKernel.FeatureFlags;

namespace Account.Workers;

/// <summary>
///     Converges the <c>feature_flags</c> table to the C# definitions in
///     <see cref="SharedKernel.FeatureFlags.FeatureFlags" /> on every worker startup. Replaces the
///     deleted one-shot <c>SeedFeatureFlags</c> and <c>SeedPlanBasedFeatureFlags</c> data migrations
///     with a converging path that handles future flag-definition changes (kill-switch flip, plan-tier
///     transitions, flag removal) without writing new data migrations.
///     For every non-System definition the reconciler ensures the global row exists with the correct
///     <see cref="FeatureFlag.Source" />, ensures the global row is active when
///     <see cref="FeatureFlagDefinition.IsKillSwitchEnabled" /> is <c>false</c>, and removes
///     stale tenant overrides whose Source no longer matches the definition (so
///     <see cref="Features.FeatureFlags.Shared.PlanBasedFeatureFlagEvaluator" /> can rebuild them on
///     the next login). Any DB row whose flag_key is not in the C# definitions is marked
///     <see cref="FeatureFlag.OrphanedAt" /> at the current time. The reconciler is idempotent — a
///     second pass on top of a converged DB produces no changes.
///     Runs inline at Worker startup (NOT a BackgroundService) so that a failure aborts the process
///     before it accepts traffic — better to fail to start than to run with inconsistent flag state.
/// </summary>
public sealed class FeatureFlagDefinitionReconciler(
    IFeatureFlagRepository featureFlagRepository,
    AccountDbContext accountDbContext,
    TimeProvider timeProvider,
    ILogger<FeatureFlagDefinitionReconciler> logger
)
{
    public async Task ReconcileAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var definitions = FeatureFlags.GetAll().Where(d => d.Scope != FeatureFlagScope.System).ToArray();

        // Load every base row up front so the per-definition reconcile loop is in-memory only. Avoids one
        // round-trip per definition; the base-row count is bounded by the C# definitions, but a single
        // query is still cheaper and matches how the rest of the codebase batches startup reads.
        var baseRowsByKey = (await featureFlagRepository.GetAllBaseRowsAsync(cancellationToken)).ToDictionary(r => r.FlagKey);

        var baseRowsCreated = 0;
        var baseRowsActivated = 0;
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

            // A previously-removed flag is back in code (rollback or intentional reintroduction). Clear
            // both timestamps so the row participates as a live flag again — historical telemetry remains
            // continuous on the same row instead of starting a new one.
            if (baseRow.OrphanedAt is not null || baseRow.DeletedAt is not null)
            {
                baseRow.Restore();
                featureFlagRepository.Update(baseRow);
                baseRowsRestored++;
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
                var staleTenantRows = await featureFlagRepository.GetTenantOverridesForFlagAsync(definition.Key, cancellationToken);
                foreach (var staleRow in staleTenantRows.Where(r => r.Source != expectedSource))
                {
                    featureFlagRepository.Remove(staleRow);
                    staleRowsRemoved++;
                }

                baseRow.SetSource(expectedSource);
                featureFlagRepository.Update(baseRow);
                sourceTransitions++;
                logger.LogInformation(
                    "Reconciler transitioned '{FlagKey}' source to '{Source}' and removed '{StaleCount}' stale tenant overrides",
                    definition.Key, expectedSource, staleTenantRows.Length
                );
            }

            if (!definition.IsKillSwitchEnabled && !baseRow.IsActive)
            {
                baseRow.Activate(now);
                featureFlagRepository.Update(baseRow);
                baseRowsActivated++;
                logger.LogInformation("Reconciler activated kill-switch-locked base row for '{FlagKey}'", definition.Key);
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
            logger.LogInformation("Reconciler marked '{FlagKey}' (id '{Id}') as orphaned", row.FlagKey, row.Id);
        }

        await accountDbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Reconciler completed: {DefinitionCount} definitions reconciled, {BaseRowsCreated} base rows created, {BaseRowsActivated} kill-switch-locked rows activated, {BaseRowsRestored} re-added rows restored, {SourceTransitions} source transitions, {StaleRowsRemoved} stale tenant rows removed, {OrphansMarked} orphans marked",
            definitions.Length, baseRowsCreated, baseRowsActivated, baseRowsRestored, sourceTransitions, staleRowsRemoved, orphansMarked
        );
    }
}
