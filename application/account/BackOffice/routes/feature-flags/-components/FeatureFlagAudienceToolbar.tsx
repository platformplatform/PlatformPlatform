import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { InputGroup, InputGroupAddon, InputGroupButton, InputGroupInput } from "@repo/ui/components/InputGroup";
import { ToggleGroup, ToggleGroupItem } from "@repo/ui/components/ToggleGroup";
import { useDebounce } from "@repo/ui/hooks/useDebounce";
import { SearchIcon, XIcon } from "lucide-react";
import { useEffect, useState } from "react";

import { FeatureFlagAudienceState } from "@/shared/lib/api/client";

import type { StateFilter } from "./stateFilter";

import { ALL_STATE_FILTER, DEFAULT_STATE_FILTER } from "./stateFilter";

interface FeatureFlagAudienceToolbarProps {
  search: string | undefined;
  searchPlaceholder: string;
  state: StateFilter | undefined;
  hasOverride: boolean;
  hideState?: boolean;
  hideHasOverride?: boolean;
  onSearchChange: (search: string | undefined) => void;
  onStateChange: (state: StateFilter | undefined) => void;
  onHasOverrideChange: (hasOverride: boolean) => void;
  children?: React.ReactNode;
}

const HAS_OVERRIDE_VALUE = "true";

// Shared toolbar for tenant- and user-audience feature flag tables. Owns the search input
// (with 500ms debounce), the State chip group, and the Has-override chip group. Audience-specific
// chip groups (Plan for tenants, Role for users) are rendered via `children`. Each caller wires its
// own URL-schema-aware navigation callbacks so this component stays unaware of route param names.
export function FeatureFlagAudienceToolbar({
  search,
  searchPlaceholder,
  state,
  hasOverride,
  hideState,
  hideHasOverride,
  onSearchChange,
  onStateChange,
  onHasOverrideChange,
  children
}: Readonly<FeatureFlagAudienceToolbarProps>) {
  const [searchInput, setSearchInput] = useState(search ?? "");
  const debouncedSearch = useDebounce(searchInput, 500);

  useEffect(() => {
    // `search` and `onSearchChange` are intentionally NOT deps. `search` would fire this effect on
    // external URL changes (Clear filters) with a stale debouncedSearch and immediately re-push the
    // old typed value. `onSearchChange` is a fresh inline arrow on every parent render, so keeping
    // it in deps causes the same regression. The companion sync effect below handles URL → input.
    // This effect only runs when the user types something new and the debounce settles.
    if ((debouncedSearch || undefined) === search) return;
    onSearchChange(debouncedSearch || undefined);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [debouncedSearch]);

  useEffect(() => {
    setSearchInput(search ?? "");
  }, [search]);

  const handleStateChange = (values: string[]) => {
    // Single-select with multi-select "last item wins" — keep the most recently clicked chip pressed.
    // Clicking the already-pressed chip would otherwise deselect to []; default back to Enabled so one chip is always on.
    const next = (values[values.length - 1] as StateFilter | undefined) ?? DEFAULT_STATE_FILTER;
    onStateChange(next === DEFAULT_STATE_FILTER ? undefined : next);
  };

  const handleHasOverrideChange = (values: string[]) => {
    onHasOverrideChange(values.length > 0);
  };

  return (
    <div className="mb-4 flex flex-wrap items-center gap-3">
      <div className="max-w-[20rem] min-w-[14rem] flex-1">
        <InputGroup>
          <InputGroupAddon>
            <SearchIcon />
          </InputGroupAddon>
          <InputGroupInput
            type="text"
            role="searchbox"
            aria-label={t`Search`}
            placeholder={searchPlaceholder}
            value={searchInput}
            onChange={(event) => setSearchInput(event.target.value)}
            onKeyDown={(event) => event.key === "Escape" && searchInput && setSearchInput("")}
          />
          {searchInput && (
            <InputGroupAddon align="inline-end">
              <InputGroupButton onClick={() => setSearchInput("")} size="icon-xs" aria-label={t`Clear search`}>
                <XIcon />
              </InputGroupButton>
            </InputGroupAddon>
          )}
        </InputGroup>
      </div>

      {!hideState && (
        <ToggleGroup
          variant="outline"
          aria-label={t`State`}
          multiple={true}
          value={[state ?? DEFAULT_STATE_FILTER]}
          onValueChange={handleStateChange}
        >
          <ToggleGroupItem value={ALL_STATE_FILTER} className="min-w-[5rem] justify-center">
            <Trans>All</Trans>
          </ToggleGroupItem>
          <ToggleGroupItem value={FeatureFlagAudienceState.Enabled} className="min-w-[5rem] justify-center">
            <Trans>Enabled</Trans>
          </ToggleGroupItem>
          <ToggleGroupItem value={FeatureFlagAudienceState.Disabled} className="min-w-[5rem] justify-center">
            <Trans>Disabled</Trans>
          </ToggleGroupItem>
        </ToggleGroup>
      )}

      {!hideHasOverride && (
        <ToggleGroup
          variant="outline"
          aria-label={t`Override`}
          multiple={true}
          value={hasOverride ? [HAS_OVERRIDE_VALUE] : []}
          onValueChange={handleHasOverrideChange}
        >
          <ToggleGroupItem value={HAS_OVERRIDE_VALUE} className="min-w-[5rem] justify-center">
            <Trans>Has override</Trans>
          </ToggleGroupItem>
        </ToggleGroup>
      )}

      {children}
    </div>
  );
}
