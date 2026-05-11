import { FeatureFlagAudienceState } from "@/shared/lib/api/client";

/**
 * URL representation of the state filter for the override sections.
 *
 * - URL param absent is the fresh-visit default and renders the Enabled chip pressed.
 * - `Enabled` / `Disabled` filter the API call to that subset.
 * - `All` is the frontend-only sentinel for "user wants both states shown". Both chips render
 *   pressed and the API call omits `State`. We persist this in the URL (rather than relying on
 *   transient component state) so refresh and deep-link round-trip the user's selection.
 */
export const ALL_STATE_FILTER = "All";

export type StateFilter = FeatureFlagAudienceState | typeof ALL_STATE_FILTER;

export const DEFAULT_STATE_FILTER: StateFilter = FeatureFlagAudienceState.Enabled;

export function toApiState(filter: StateFilter | undefined): FeatureFlagAudienceState | undefined {
  if (filter === ALL_STATE_FILTER) return undefined;
  return filter ?? FeatureFlagAudienceState.Enabled;
}

export function toToggleValues(filter: StateFilter | undefined): FeatureFlagAudienceState[] {
  if (filter === ALL_STATE_FILTER) return [FeatureFlagAudienceState.Enabled, FeatureFlagAudienceState.Disabled];
  return [filter ?? FeatureFlagAudienceState.Enabled];
}

export function fromToggleValues(values: string[]): StateFilter {
  if (values.length === 1) return values[0] as FeatureFlagAudienceState;
  return ALL_STATE_FILTER;
}
