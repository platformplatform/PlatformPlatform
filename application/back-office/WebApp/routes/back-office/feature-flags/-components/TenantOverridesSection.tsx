import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Table, TableBody, TableHead, TableHeader, TableRow } from "@repo/ui/components/Table";
import { TextField } from "@repo/ui/components/TextField";
import { useMemo, useState } from "react";

import type { FlagTenantInfo } from "./types";

import { TenantOverrideRow } from "./TenantOverrideRow";

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
          (tenant) => tenant.tenantName.toLowerCase().includes(lowerSearch) || tenant.tenantId.includes(lowerSearch)
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
