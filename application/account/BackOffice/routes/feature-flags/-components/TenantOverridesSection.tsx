import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from "@repo/ui/components/Collapsible";
import { TextField } from "@repo/ui/components/TextField";
import { ChevronDown } from "lucide-react";
import { useMemo, useState } from "react";

import type { RolloutBucketRange } from "./rolloutBucket";
import type { FeatureFlagTenantInfo } from "./types";

import { sortBySourceThenRolloutBucket } from "./rolloutBucket";
import { TenantOverrideTable, type TenantOverrideTableProps } from "./TenantOverrideTable";

export function TenantOverridesSection({
  flagKey,
  featureFlagDescription,
  tenants,
  showRolloutBucket,
  rolloutBucketRange,
  isFeatureFlagActive
}: Readonly<{
  flagKey: string;
  featureFlagDescription: string;
  tenants: FeatureFlagTenantInfo[];
  showRolloutBucket: boolean;
  rolloutBucketRange: RolloutBucketRange | null;
  isFeatureFlagActive: boolean;
}>) {
  const [search, setSearch] = useState("");

  const filtered = useMemo(() => {
    const lowerSearch = search.toLowerCase();
    return search
      ? tenants.filter((tenant) => tenant.name.toLowerCase().includes(lowerSearch) || tenant.id.includes(lowerSearch))
      : tenants;
  }, [tenants, search]);

  const enabledTenants = useMemo(
    () =>
      sortBySourceThenRolloutBucket(
        filtered.filter((t) => t.isEnabled),
        (t) => t.source,
        (t) => t.rolloutBucket,
        "enabled",
        rolloutBucketRange
      ),
    [filtered, rolloutBucketRange]
  );

  const disabledTenants = useMemo(
    () =>
      sortBySourceThenRolloutBucket(
        filtered.filter((t) => !t.isEnabled),
        (t) => t.source,
        (t) => t.rolloutBucket,
        "disabled",
        rolloutBucketRange
      ),
    [filtered, rolloutBucketRange]
  );

  const isSearching = search.length > 0;

  return (
    <div className="flex flex-col gap-4">
      <div>
        <h3>
          <Trans>Account status</Trans>
        </h3>
        <p className="text-sm text-muted-foreground">
          {showRolloutBucket ? (
            <Trans>
              Accounts are automatically included based on their rollout bucket. Use overrides to manually include or
              exclude specific accounts.
            </Trans>
          ) : (
            <Trans>Toggle the override switch to enable this feature for specific accounts.</Trans>
          )}
        </p>
      </div>
      <TextField
        name="search"
        placeholder={t`Search by account name or ID`}
        value={search}
        onChange={(value) => setSearch(value)}
        className="max-w-[20rem]"
      />
      {isSearching ? (
        <TenantOverrideTable
          ariaLabel={t`Search results`}
          tenants={[...enabledTenants, ...disabledTenants]}
          flagKey={flagKey}
          featureFlagDescription={featureFlagDescription}
          showRolloutBucket={showRolloutBucket}
          isFeatureFlagActive={isFeatureFlagActive}
        />
      ) : (
        <>
          <CollapsibleTenantGroup
            label={t`Enabled (${enabledTenants.length})`}
            tenants={enabledTenants}
            flagKey={flagKey}
            featureFlagDescription={featureFlagDescription}
            showRolloutBucket={showRolloutBucket}
            isFeatureFlagActive={isFeatureFlagActive}
          />
          <CollapsibleTenantGroup
            label={t`Disabled (${disabledTenants.length})`}
            tenants={disabledTenants}
            flagKey={flagKey}
            featureFlagDescription={featureFlagDescription}
            showRolloutBucket={showRolloutBucket}
            isFeatureFlagActive={isFeatureFlagActive}
          />
        </>
      )}
    </div>
  );
}

function CollapsibleTenantGroup({
  label,
  ...tableProps
}: Readonly<{ label: string } & Omit<TenantOverrideTableProps, "ariaLabel">>) {
  const [isOpen, setIsOpen] = useState(true);

  return (
    <Collapsible open={isOpen} onOpenChange={setIsOpen} className="flex flex-col gap-1">
      <CollapsibleTrigger className="flex cursor-pointer items-center gap-1 text-left">
        <ChevronDown
          className={`size-4 text-muted-foreground transition ${isOpen ? "" : "-rotate-90"}`}
          aria-hidden={true}
        />
        <h4 className="text-muted-foreground">{label}</h4>
      </CollapsibleTrigger>
      <CollapsibleContent>
        {tableProps.tenants.length > 0 ? (
          <TenantOverrideTable ariaLabel={label} {...tableProps} />
        ) : (
          <p className="py-2 text-sm text-muted-foreground">
            <Trans>No accounts in this group.</Trans>
          </p>
        )}
      </CollapsibleContent>
    </Collapsible>
  );
}
