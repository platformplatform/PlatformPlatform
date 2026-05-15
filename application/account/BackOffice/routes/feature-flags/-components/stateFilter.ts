import { FeatureFlagAudienceState } from "@/shared/lib/api/client";

/**
 * URL representation of the single-select state filter on the override toolbars.
 *
 * - URL param absent is the fresh-visit default and renders the Enabled chip pressed.
 * - `All` shows all rows: API call omits `State`, UI marks the All chip pressed.
 * - `Enabled` / `Disabled` filter the API call to that subset.
 */
export const ALL_STATE_FILTER = "All";

export type StateFilter = FeatureFlagAudienceState | typeof ALL_STATE_FILTER;

export const DEFAULT_STATE_FILTER: StateFilter = FeatureFlagAudienceState.Enabled;

export function toApiState(filter: StateFilter | undefined): FeatureFlagAudienceState | undefined {
  if (filter === ALL_STATE_FILTER) return undefined;
  return filter ?? FeatureFlagAudienceState.Enabled;
}
