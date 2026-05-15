---
description: Non-obvious behaviour of the feature flag system on the backend (declaration, evaluation, lifecycle, ownership)
---

# Feature Flags (backend)

Load this when adding, removing, or changing how a feature flag is evaluated or stored. The obvious parts (declare a field, read `executionContext.UserInfo.FeatureFlags`) are not repeated.

## Subtype Is the Contract

The `FeatureFlagDefinition` subtype on a field is not just a permissions tag — it rewires database-row ownership and validator behaviour at runtime. The subtype hierarchy replaced what used to be runtime checks (see the comment above the registry in `application/shared-kernel/SharedKernel/FeatureFlags/FeatureFlags.cs`).

- `PlanGatedTenantFlag` makes `PlanBasedFeatureFlagEvaluator` the exclusive writer of tenant overrides on every JWT refresh; the reconciler stamps `Source=Plan` on the base row and the Set/Remove validators block manual edits.
- `SystemFeatureFlag` skips the database entirely — config + frontend env var only. There is no per-tenant override.
- `TenantAbTestFlag` / `UserAbTestFlag` enable rollout buckets; `IsAbTestEligible` and `ConfigurableBy*` are mutually exclusive by construction (the disjoint subtype branches), and `BucketStart/End` are ignored if you pick a non-AB subtype.
- `TenantOwnerConfigurableFlag` requires `AdminLevel.TenantOwner` and `Scope.Tenant`; handlers check both. If those ever drift you get a silent 403.

Switching subtypes on an existing flag changes the row owner on the next reconcile — confirm that's what you want before flipping a `TenantAbTestFlag` to `PlanGatedTenantFlag`.

## `IsKillSwitchEnabled` Defaults the Row to Inactive

`isKillSwitchEnabled: true` causes the reconciler to create the base row with `EnabledAt = null` — an admin must click Activate before anyone sees the flag. `ActivateFeatureFlag` / `DeactivateFeatureFlag` only operate on kill-switch flags; default-false flags (e.g. `TenantOwnerConfigurableFlag`) are globally un-killable by design — only per-tenant overrides can turn them off.

## Soft-Delete Burns the Key

The startup reconciler marks orphaned rows (`OrphanedAt`), and re-adding the same key restores them — including any orphaned tenant/user overrides. **But** once a row is `DeletedAt`-stamped (back-office hard-delete), re-adding the same key in C# throws on startup and aborts deployment. Don't re-use a name; pick a new one.

## The Four BackOffice Query Mirrors Drift Silently

`FeatureFlagEvaluator` is the canonical runtime path. The four BackOffice query handlers (`GetUserFeatureFlags`, `GetTenantFeatureFlags`, `GetFeatureFlagUsers`, `GetFeatureFlagTenants`) each carry their own copy of `EvaluateAbRollout`, `ComputeInclusionThresholdPercentage`, and `ComputeDefaultEnabled`. They agree today by construction, not by tests — if you change evaluation math, update all five and rely on `FeatureFlagEvaluatorTests` for the canonical contract.

Known divergences worth being aware of: the mirrors do NOT consult parent-dependency, and `EvaluateOverride` in the bulk-list mirrors returns `IsEnabled=true` without checking `baseRow.IsActive`. BackOffice display can therefore report a flag as enabled while runtime evaluation excludes it.

## Disable Semantics Are Asymmetric

The four "disable this flag for this entity" paths look symmetric but aren't:

- `SetTenantFeatureFlagInternal(Enabled=false)` (admin) creates a new override row with `EnabledAt == DisabledAt` if none exists. This is required: without it, the entity would stay enabled-by-rollout. Same applies to `SetUserFeatureFlagInternal`.
- `SetTenantFeatureFlagOwner(Enabled=false)` (tenant owner) no-ops if no override exists — owners can't yank themselves out of a rollout they were never explicitly in.
- `RemoveTenantFeatureFlagOverride` (admin only) is a hard `Remove`; the row is dropped.
- `SetTenantFeatureFlagInternal(Enabled=false)` is a soft `Deactivate`; the row is kept with `DisabledAt` set.

Both removal paths emit `FeatureFlagTenantOverrideRemoved`, but they produce different `OverrideCount` results in bulk admin lists because the list counts `Source=Manual` rows — `Remove` drops them, `Deactivate` keeps them.

`SetTenantFeatureFlagOwner` is NOT a back-office mutation. It lives under `/api/account/feature-flags/{key}/tenant-override` and self-validates `Role == Owner`. Plan-gated flags are explicitly blocked at the validator level for both manual paths.

## Rollout Math Is Deterministic by Flag

`RolloutBucketHasher` is a van der Corput low-discrepancy sequence offset by a per-flag FNV-1a hash of the key. Two flags at the same percentage do NOT cover the same tenants, and ramping up never reshuffles existing members — a tenant included at 10% stays included at 20%. Don't replace this with a random or modulo strategy.

`ComputeInclusionThresholdPercentage` returns the percentage at which an entity would join the rollout, but it's special-cased for pins (`AlwaysOn` → 0, `NeverOn` → null). After the pin-trumps-rollout change, pins are unconditional, so the "joins at N%" column in BackOffice lists is meaningless for pinned rows.

## Reading vs Writing

- Handlers: read `executionContext.UserInfo.FeatureFlags` (already populated from the JWT claim). Re-querying the DB at request time means you've stepped outside the JWT-claim contract.
- The `FeatureFlagEvaluator` runs at JWT refresh, not per request. Flag changes take up to the 5-minute access-token TTL to propagate.
- Mutations that change the actor's own claim must chain `AddRefreshAuthenticationTokens()` so the gateway refreshes the JWT in-flight; without that the user waits up to 5 minutes. Plain mutations of other users' flags don't need this — they'll see it on their next refresh.

## Architecture Test Guards

Every new back-office mutation belongs in `EndpointMetadataTests.AdminPolicyBackOfficeRoutes` if admin-only, and gets a paired `_WhenNonAdminBackOfficeIdentity_ShouldReturnForbidden` test. The architecture test fails if a route's declared `RequireAuthorization` policy doesn't match the allowlist.

## Telemetry

Override events (`FeatureFlagTenantOverrideSet/Removed`, `FeatureFlagUserOverrideSet/Removed`) carry a `FeatureFlagOverrideTrigger` axis: `Internal` (back-office staff), `Owner` (tenant owner), `Self` (end-user preference). Plan-source transitions emit their own `FeatureFlagPlanOverrideActivated/Deactivated` events — they're not in the trigger enum. Per-flag telemetry tags are emitted as a single comma-separated `user.feature_flags` dimension; `feature_flag.*` is reserved by the OTel semantic-conventions registry, do not reintroduce it.
