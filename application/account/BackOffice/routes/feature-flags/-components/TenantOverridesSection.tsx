import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Table, TableBody, TableHead, TableHeader, TableRow } from "@repo/ui/components/Table";
import { TextField } from "@repo/ui/components/TextField";
import { ChevronDown } from "lucide-react";
import { useMemo, useState } from "react";

import type { BucketRange } from "./rolloutBucket";
import type { FlagTenantInfo } from "./types";

import { sortBySourceThenBucket } from "./rolloutBucket";
import { TenantOverrideRow } from "./TenantOverrideRow";

export function TenantOverridesSection({
  flagKey,
  flagDescription,
  tenants,
  showBucket,
  bucketRange,
  isFlagActive
}: Readonly<{
  flagKey: string;
  flagDescription: string;
  tenants: FlagTenantInfo[];
  showBucket: boolean;
  bucketRange: BucketRange | null;
  isFlagActive: boolean;
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
      sortBySourceThenBucket(
        filtered.filter((t) => t.isEnabled),
        (t) => t.source,
        (t) => t.tenantId,
        "enabled",
        bucketRange
      ),
    [filtered, bucketRange]
  );

  const disabledTenants = useMemo(
    () =>
      sortBySourceThenBucket(
        filtered.filter((t) => !t.isEnabled),
        (t) => t.source,
        (t) => t.tenantId,
        "disabled",
        bucketRange
      ),
    [filtered, bucketRange]
  );

  const isSearching = search.length > 0;

  return (
    <div className="flex flex-col gap-4">
      <h3>
        <Trans>Account status</Trans>
      </h3>
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
          flagDescription={flagDescription}
          showBucket={showBucket}
          isFlagActive={isFlagActive}
        />
      ) : (
        <>
          <CollapsibleTenantGroup
            label={t`Enabled (${enabledTenants.length})`}
            tenants={enabledTenants}
            flagKey={flagKey}
            flagDescription={flagDescription}
            showBucket={showBucket}
            isFlagActive={isFlagActive}
          />
          <CollapsibleTenantGroup
            label={t`Disabled (${disabledTenants.length})`}
            tenants={disabledTenants}
            flagKey={flagKey}
            flagDescription={flagDescription}
            showBucket={showBucket}
            isFlagActive={isFlagActive}
          />
        </>
      )}
    </div>
  );
}

interface TenantTableProps {
  ariaLabel: string;
  tenants: FlagTenantInfo[];
  flagKey: string;
  flagDescription: string;
  showBucket: boolean;
  isFlagActive: boolean;
}

function TenantTable({
  ariaLabel,
  tenants,
  flagKey,
  flagDescription,
  showBucket,
  isFlagActive
}: Readonly<TenantTableProps>) {
  return (
    <Table rowSize="compact" aria-label={ariaLabel}>
      <TableHeader>
        <TableRow>
          <TableHead className="hidden w-[12rem] sm:table-cell">
            <Trans>Account ID</Trans>
          </TableHead>
          <TableHead>
            <Trans>Account</Trans>
          </TableHead>
          <TableHead className="w-[6rem]">
            <Trans>Plan</Trans>
          </TableHead>
          <TableHead className="hidden w-[8rem] sm:table-cell">
            <Trans>Source</Trans>
          </TableHead>
          {showBucket && (
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
            flagDescription={flagDescription}
            tenant={tenant}
            showBucket={showBucket}
            isFlagActive={isFlagActive}
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
      {isOpen && <TenantTable ariaLabel={label} {...tableProps} />}
    </div>
  );
}
