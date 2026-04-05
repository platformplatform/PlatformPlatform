import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Table, TableBody, TableHead, TableHeader, TableRow } from "@repo/ui/components/Table";
import { TextField } from "@repo/ui/components/TextField";
import { ChevronDown } from "lucide-react";
import { useMemo, useState } from "react";

import type { RolloutBucketRange } from "./rolloutBucket";
import type { FeatureFlagTenantInfo } from "./types";

import { sortBySourceThenRolloutBucket } from "./rolloutBucket";
import { TenantOverrideRow } from "./TenantOverrideRow";

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
      ? tenants.filter(
          (tenant) => tenant.tenantName.toLowerCase().includes(lowerSearch) || tenant.tenantId.includes(lowerSearch)
        )
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
        <TenantTable
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

interface TenantTableProps {
  ariaLabel: string;
  tenants: FeatureFlagTenantInfo[];
  flagKey: string;
  featureFlagDescription: string;
  showRolloutBucket: boolean;
  isFeatureFlagActive: boolean;
}

function TenantTable({
  ariaLabel,
  tenants,
  flagKey,
  featureFlagDescription,
  showRolloutBucket,
  isFeatureFlagActive
}: Readonly<TenantTableProps>) {
  return (
    <Table rowSize="compact" aria-label={ariaLabel} className="table-fixed">
      <TableHeader>
        <TableRow>
          <TableHead className="hidden w-[14rem] lg:table-cell">
            <Trans>Account ID</Trans>
          </TableHead>
          <TableHead className="w-auto">
            <Trans>Account</Trans>
          </TableHead>
          <TableHead className="w-[5rem]">
            <Trans>Plan</Trans>
          </TableHead>
          <TableHead className="hidden w-[8rem] sm:table-cell">
            <Trans>Source</Trans>
          </TableHead>
          {showRolloutBucket && (
            <TableHead className="hidden w-[5rem] sm:table-cell">
              <Trans>Bucket</Trans>
            </TableHead>
          )}
          <TableHead className="w-[7rem] text-right">
            <Trans>Override</Trans>
          </TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {tenants.map((tenant) => (
          <TenantOverrideRow
            key={tenant.tenantId}
            flagKey={flagKey}
            featureFlagDescription={featureFlagDescription}
            tenant={tenant}
            showRolloutBucket={showRolloutBucket}
            isFeatureFlagActive={isFeatureFlagActive}
          />
        ))}
      </TableBody>
    </Table>
  );
}

function CollapsibleTenantGroup({
  label,
  ...tableProps
}: Readonly<{ label: string } & Omit<TenantTableProps, "ariaLabel">>) {
  const [isOpen, setIsOpen] = useState(true);

  return (
    <div className="flex flex-col gap-1">
      <button
        type="button"
        className="flex cursor-pointer items-center gap-1 text-left"
        onClick={() => setIsOpen((prev) => !prev)}
        aria-expanded={isOpen}
      >
        <ChevronDown
          className={`size-4 text-muted-foreground transition ${isOpen ? "" : "-rotate-90"}`}
          aria-hidden={true}
        />
        <h4 className="text-muted-foreground">{label}</h4>
      </button>
      {isOpen &&
        (tableProps.tenants.length > 0 ? (
          <TenantTable ariaLabel={label} {...tableProps} />
        ) : (
          <p className="py-2 text-sm text-muted-foreground">
            <Trans>No accounts in this group.</Trans>
          </p>
        ))}
    </div>
  );
}
