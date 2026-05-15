---
description: Non-obvious behaviour of the feature flag system on the frontend (hook, codegen, propagation timing)
---

# Feature Flags (frontend)

Load this when gating UI on a feature flag, displaying a flag, or wondering why a toggle isn't reflecting in the SPA. The obvious parts (`useFeatureFlag(<key>)` returns `{ enabled }`) are not repeated.

## `FeatureFlagKey` Is a Type-Level Contract

`<key>` is typed against `FeatureFlagKey`, a codegen string-literal union built from `SharedKernel.FeatureFlags.FeatureFlags.cs`. Never cast a string to `FeatureFlagKey`; never accept a `string` parameter where `FeatureFlagKey` would do. Removing or renaming a flag in C# turns every stale callsite into a TS compile error after the next backend build — that compile error is the safety net.

The codegen also enforces backend-before-frontend deploy order: the frontend build cannot reference a flag the backend hasn't shipped because the union is regenerated from the C# manifest.

## The Hook Doesn't Subscribe — `AuthenticationProvider` Does

`useFeatureFlag` reads `useUserInfo().featureFlags`. The bridge that turns the `x-user-feature-flags` response header into a re-render lives in `AuthenticationProvider` and short-circuits on identical flag sets, so re-renders are cheap. **Every authenticated response is the eventing channel** — there is no push, no SSE, no polling, and there is no flag-specific TanStack query to invalidate. Don't call `queryClient.invalidateQueries` for flag changes.

## System Flags Bypass the User Path

For `Scope: "system"` flags the hook reads `import.meta.runtime_env[envVar]` and ignores `userInfo` entirely. The hook handles this transparently — just call `useFeatureFlag(<key>)` regardless of scope. There is no per-tenant or per-user override for a system flag; if you need that, the flag is the wrong subtype on the backend.

## Propagation Has a 5-Minute Floor

Flag state lags behind a back-office or self-service toggle by up to the 5-minute access-token TTL. The mutation response carries `x-user-feature-flags` only when the mutating endpoint chains `AddRefreshAuthenticationTokens()` AND the gateway's endpoint-triggered refresh succeeds. Don't optimistically update for flag-driven UI; the response is the source of truth. If the backend was transiently unavailable during the refresh, the gateway suppresses `x-user-feature-flags` rather than emit the stale claim — so a "no change visible after toggle" outcome is a possible (rare) UI state.

## Labels Are Codegen Too

Display copy lives in `@repo/ui/featureFlags/labels` (`labels.generated.ts`), sourced from each `FeatureFlagDefinition.Label` / `Description`. Don't write parallel Lingui strings for flag names in components — call `getFeatureFlagLabel(key)`. The labels participate in the shared Lingui catalog under `shared-webapp/ui/translations/locale/`; translate there, not in the per-system catalog.

## Where the Surfaces Live

- Owner self-service: `/account/settings` (Features section) — `TenantOwnerConfigurableFlag` only.
- User preferences: same area — `UserConfigurableFlag` only.
- Back-office admin: `/feature-flags/{key}` — every scope.

Orphaned and soft-deleted flags surface read-only in the back-office. They never reach `useFeatureFlag` (the codegen union drops them on the next backend build), so call sites don't need a "deleted flag" branch.
