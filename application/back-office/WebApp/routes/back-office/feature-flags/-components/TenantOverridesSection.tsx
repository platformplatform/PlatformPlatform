import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Badge } from "@repo/ui/components/Badge";
import { Switch } from "@repo/ui/components/Switch";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@repo/ui/components/Table";
import { TextField } from "@repo/ui/components/TextField";
import { useMemo, useState } from "react";
import { toast } from "sonner";

import { api } from "@/shared/lib/api/client";

import type { FlagTenantInfo } from "./types";

type SortColumn = "tenantName" | "isEnabled";
type SortDirection = "ascending" | "descending";

export function TenantOverridesSection({
  flagKey,
  flagDescription,
  tenants
}: Readonly<{
  flagKey: string;
  flagDescription: string;
  tenants: FlagTenantInfo[];
}>) {
  const [search, setSearch] = useState("");
  const [sortColumn, setSortColumn] = useState<SortColumn>("tenantName");
  const [sortDirection, setSortDirection] = useState<SortDirection>("ascending");

  const filteredAndSortedTenants = useMemo(() => {
    const lowerSearch = search.toLowerCase();
    const filtered = search
      ? tenants.filter(
          (tenant) =>
            tenant.tenantName.toLowerCase().includes(lowerSearch) || String(tenant.tenantId).includes(lowerSearch)
        )
      : tenants;

    return [...filtered].sort((a, b) => {
      const direction = sortDirection === "ascending" ? 1 : -1;
      if (sortColumn === "tenantName") {
        return a.tenantName.localeCompare(b.tenantName) * direction;
      }
      return (Number(b.isEnabled) - Number(a.isEnabled)) * direction;
    });
  }, [tenants, search, sortColumn, sortDirection]);

  const handleSortChange = (column: SortColumn) => {
    if (sortColumn === column) {
      setSortDirection((prev) => (prev === "ascending" ? "descending" : "ascending"));
    } else {
      setSortColumn(column);
      setSortDirection("ascending");
    }
  };

  return (
    <div className="flex flex-col gap-4">
      <h3>
        <Trans>Tenant overrides</Trans>
      </h3>
      <TextField
        name="search"
        placeholder={t`Search by tenant name or ID`}
        value={search}
        onChange={(value) => setSearch(value)}
        className="max-w-[20rem]"
      />
      <div className="rounded-md border">
        <Table rowSize="compact" aria-label={t`Tenant overrides`}>
          <TableHeader>
            <TableRow>
              <TableHead>
                <Trans>Tenant ID</Trans>
              </TableHead>
              <TableHead
                className="cursor-pointer select-none"
                onClick={() => handleSortChange("tenantName")}
                aria-sort={sortColumn === "tenantName" ? sortDirection : undefined}
              >
                <Trans>Tenant</Trans>
                {sortColumn === "tenantName" && <SortIndicator direction={sortDirection} />}
              </TableHead>
              <TableHead
                className="cursor-pointer select-none"
                onClick={() => handleSortChange("isEnabled")}
                aria-sort={sortColumn === "isEnabled" ? sortDirection : undefined}
              >
                <Trans>Status</Trans>
                {sortColumn === "isEnabled" && <SortIndicator direction={sortDirection} />}
              </TableHead>
              <TableHead>
                <Trans>Source</Trans>
              </TableHead>
              <TableHead className="text-right">
                <Trans>Override</Trans>
              </TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {filteredAndSortedTenants.map((tenant) => (
              <TenantOverrideRow
                key={tenant.tenantId}
                flagKey={flagKey}
                flagDescription={flagDescription}
                tenant={tenant}
              />
            ))}
          </TableBody>
        </Table>
      </div>
    </div>
  );
}

function SortIndicator({ direction }: Readonly<{ direction: SortDirection }>) {
  return <span className="ml-1">{direction === "ascending" ? "\u25B2" : "\u25BC"}</span>;
}

function TenantOverrideRow({
  flagKey,
  flagDescription,
  tenant
}: Readonly<{
  flagKey: string;
  flagDescription: string;
  tenant: FlagTenantInfo;
}>) {
  const overrideMutation = api.useMutation("put", "/api/back-office/feature-flags/{flagKey}/tenant-override");

  const handleToggle = (checked: boolean) => {
    overrideMutation.mutate(
      {
        params: { path: { flagKey } },
        body: { tenantId: tenant.tenantId, enabled: checked }
      },
      {
        onSuccess: () => {
          const message = checked
            ? t`${flagDescription} enabled for ${tenant.tenantName}`
            : t`${flagDescription} disabled for ${tenant.tenantName}`;
          toast.success(message);
        }
      }
    );
  };

  const sourceLabel = getSourceLabel(tenant.source);

  return (
    <TableRow>
      <TableCell className="text-muted-foreground">{tenant.tenantId}</TableCell>
      <TableCell className="font-medium">{tenant.tenantName}</TableCell>
      <TableCell>
        <Badge variant={tenant.isEnabled ? "default" : "outline"}>{tenant.isEnabled ? t`Enabled` : t`Disabled`}</Badge>
      </TableCell>
      <TableCell>
        <span className="text-sm text-muted-foreground">{sourceLabel}</span>
      </TableCell>
      <TableCell className="text-right">
        <Switch
          checked={tenant.isEnabled}
          onCheckedChange={handleToggle}
          disabled={overrideMutation.isPending}
          aria-label={t`Override for ${tenant.tenantName}`}
        />
      </TableCell>
    </TableRow>
  );
}

function getSourceLabel(source: string): string {
  switch (source) {
    case "manual_override":
      return t`Manual override`;
    case "ab_rollout":
      return t`A/B rollout`;
    case "default":
      return t`Default`;
    default:
      return source;
  }
}
