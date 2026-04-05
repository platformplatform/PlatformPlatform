import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Badge } from "@repo/ui/components/Badge";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@repo/ui/components/Table";
import { TextField } from "@repo/ui/components/TextField";
import { ChevronDown } from "lucide-react";
import { useMemo, useState } from "react";

import type { FeatureFlagInfo, FlagTenantInfo } from "./types";

export function PlanFlagInfoSection({ flag }: Readonly<{ flag: FeatureFlagInfo }>) {
  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div className="flex flex-col gap-0.5 text-sm text-muted-foreground">
          <span>
            <Trans>Name:</Trans> <span className="font-mono">{flag.key}</span>
          </span>
          <span>
            <Trans>Required plan:</Trans> <Badge variant="outline">{flag.requiredPlan}</Badge>
          </span>
        </div>
        <Badge variant={flag.isActive ? "default" : "outline"}>{flag.isActive ? t`Active` : t`Inactive`}</Badge>
      </div>
      <p className="text-sm text-muted-foreground">
        <Trans>
          This flag is managed by the subscription plan. It is automatically enabled for accounts on the required plan
          or higher.
        </Trans>
      </p>
    </div>
  );
}

export function PlanFlagTenantsSection({ tenants }: Readonly<{ tenants: FlagTenantInfo[] }>) {
  const [search, setSearch] = useState("");

  const filtered = useMemo(() => {
    const lowerSearch = search.toLowerCase();
    return search
      ? tenants.filter(
          (tenant) => tenant.tenantName.toLowerCase().includes(lowerSearch) || tenant.tenantId.includes(lowerSearch)
        )
      : tenants;
  }, [tenants, search]);

  const planGroups = useMemo(() => {
    const groupMap = new Map<string, FlagTenantInfo[]>();
    for (const tenant of filtered) {
      const existing = groupMap.get(tenant.plan);
      if (existing) {
        existing.push(tenant);
      } else {
        groupMap.set(tenant.plan, [tenant]);
      }
    }
    return [...groupMap.entries()].map(([plan, members]) => ({ plan, tenants: members }));
  }, [filtered]);

  const isSearching = search.length > 0;

  return (
    <div className="flex flex-col gap-4">
      <div>
        <h3>
          <Trans>Account status</Trans>
        </h3>
        <p className="text-sm text-muted-foreground">
          <Trans>
            Accounts are automatically enabled or disabled based on their subscription plan. No manual overrides are
            available for plan-managed flags.
          </Trans>
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
        <PlanFlagTenantTable ariaLabel={t`Search results`} tenants={filtered} />
      ) : (
        planGroups.map((group) => (
          <CollapsiblePlanGroup
            key={group.plan}
            label={t`${group.plan} (${group.tenants.length})`}
            tenants={group.tenants}
          />
        ))
      )}
    </div>
  );
}

function PlanFlagTenantTable({ ariaLabel, tenants }: Readonly<{ ariaLabel: string; tenants: FlagTenantInfo[] }>) {
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
          <TableHead className="w-[6rem] text-right">
            <Trans>Status</Trans>
          </TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {tenants.map((tenant) => (
          <TableRow key={tenant.tenantId}>
            <TableCell className="hidden truncate text-muted-foreground lg:table-cell">{tenant.tenantId}</TableCell>
            <TableCell className="truncate font-medium">{tenant.tenantName}</TableCell>
            <TableCell className="text-muted-foreground">{tenant.plan}</TableCell>
            <TableCell className="text-right">
              <Badge variant={tenant.isEnabled ? "default" : "outline"}>
                {tenant.isEnabled ? t`Enabled` : t`Disabled`}
              </Badge>
            </TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  );
}

function CollapsiblePlanGroup({ label, tenants }: Readonly<{ label: string; tenants: FlagTenantInfo[] }>) {
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
      {isOpen && <PlanFlagTenantTable ariaLabel={label} tenants={tenants} />}
    </div>
  );
}
