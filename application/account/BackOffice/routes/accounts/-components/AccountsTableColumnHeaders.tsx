import { Trans } from "@lingui/react/macro";
import { TableHeader, TableRow } from "@repo/ui/components/Table";

import type { SortOrder } from "@/shared/lib/api/client";

import { SortableTenantProperties } from "@/shared/lib/api/client";

import { SortableTableHead } from "./SortableTableHead";

interface AccountsTableColumnHeadersProps {
  orderBy: SortableTenantProperties | undefined;
  sortOrder: SortOrder | undefined;
  onSort: (column: SortableTenantProperties) => void;
}

export function AccountsTableColumnHeaders({ orderBy, sortOrder, onSort }: Readonly<AccountsTableColumnHeadersProps>) {
  return (
    <TableHeader className="z-10 bg-inherit sm:sticky sm:top-0">
      <TableRow>
        <SortableTableHead
          column={SortableTenantProperties.Name}
          orderBy={orderBy}
          sortOrder={sortOrder}
          onSort={onSort}
          className="w-[12rem] min-w-[12rem] md:w-[14rem] md:min-w-[14rem]"
        >
          <Trans>Name</Trans>
        </SortableTableHead>
        <SortableTableHead
          column={SortableTenantProperties.Plan}
          orderBy={orderBy}
          sortOrder={sortOrder}
          onSort={onSort}
          className="hidden w-[5.5rem] lg:table-cell"
        >
          <Trans>Plan</Trans>
        </SortableTableHead>
        <SortableTableHead
          column={SortableTenantProperties.MonthlyRecurringRevenue}
          orderBy={orderBy}
          sortOrder={sortOrder}
          onSort={onSort}
          className="hidden w-[6rem] md:table-cell"
        >
          <Trans>MRR</Trans>
        </SortableTableHead>
        <SortableTableHead
          column={SortableTenantProperties.RenewalDate}
          orderBy={orderBy}
          sortOrder={sortOrder}
          onSort={onSort}
          className="hidden w-[6.5rem] lg:table-cell"
        >
          <Trans>Renewal</Trans>
        </SortableTableHead>
        <SortableTableHead
          column={SortableTenantProperties.Status}
          orderBy={orderBy}
          sortOrder={sortOrder}
          onSort={onSort}
          className="hidden w-[7.5rem] md:table-cell"
        >
          <Trans>Status</Trans>
        </SortableTableHead>
        <SortableTableHead
          column={SortableTenantProperties.Country}
          orderBy={orderBy}
          sortOrder={sortOrder}
          onSort={onSort}
          className="hidden w-[5rem] xl:table-cell"
        >
          <Trans>Country</Trans>
        </SortableTableHead>
        <SortableTableHead
          column={SortableTenantProperties.CreatedAt}
          orderBy={orderBy}
          sortOrder={sortOrder}
          onSort={onSort}
          className="hidden w-[6.5rem] xl:table-cell"
        >
          <Trans>Signed up</Trans>
        </SortableTableHead>
      </TableRow>
    </TableHeader>
  );
}
