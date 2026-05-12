import type { RowKey } from "@repo/ui/components/Table";

import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { Table, TableBody, TableHead, TableHeader, TableRow } from "@repo/ui/components/Table";
import { TablePagination } from "@repo/ui/components/TablePagination";
import { useFormatDate } from "@repo/ui/hooks/useSmartDate";
import { useNavigate } from "@tanstack/react-router";
import { useCallback } from "react";

import type { components } from "@/shared/lib/api/client";

import { UsersTableRow } from "./UsersTableRow";

type BackOfficeUserSummary = components["schemas"]["BackOfficeUserSummary"];

interface UsersTableProps {
  users: BackOfficeUserSummary[];
  isLoading: boolean;
  totalPages: number;
  currentPageOffset: number;
}

export function UsersTable({ users, isLoading, totalPages, currentPageOffset }: Readonly<UsersTableProps>) {
  const navigate = useNavigate();
  const formatDate = useFormatDate();

  const handleActivate = useCallback(
    (key: RowKey) => {
      const user = users.find((entry) => entry.id === key);
      if (!user) return;
      navigate({ to: "/users/$userId", params: { userId: user.id } });
    },
    [navigate, users]
  );

  const handlePageChange = useCallback(
    (page: number) => {
      navigate({
        to: "/users",
        search: (previous) => ({
          ...previous,
          pageOffset: page === 1 ? undefined : page - 1
        })
      });
    },
    [navigate]
  );

  if (isLoading && users.length === 0) {
    return (
      <div className="flex flex-1 flex-col gap-2 p-2">
        {Array.from({ length: 8 }).map((_, index) => (
          <Skeleton key={`skeleton-${index}`} className="h-12 w-full" />
        ))}
      </div>
    );
  }

  const currentPage = currentPageOffset + 1;

  return (
    <>
      <div className="flex-1 overflow-visible sm:min-h-48 sm:overflow-auto">
        <Table
          className="table-fixed"
          rowSize="spacious"
          aria-label={t`Users`}
          selectionMode="single"
          onActivate={handleActivate}
          stickyHeader={true}
        >
          <TableHeader>
            <TableRow>
              <TableHead>
                <Trans>User</Trans>
              </TableHead>
              <TableHead className="hidden md:table-cell">
                <Trans>Account</Trans>
              </TableHead>
              <TableHead className="hidden lg:table-cell">
                <Trans>Role</Trans>
              </TableHead>
              <TableHead className="hidden lg:table-cell">
                <Trans>Last seen</Trans>
              </TableHead>
              <TableHead className="hidden xl:table-cell">
                <Trans>Created</Trans>
              </TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {users.map((user) => (
              <UsersTableRow key={user.id} user={user} formatDate={formatDate} />
            ))}
          </TableBody>
        </Table>
      </div>

      {totalPages > 1 && (
        <div className="shrink-0 pt-4">
          <TablePagination
            currentPage={currentPage}
            totalPages={totalPages}
            onPageChange={handlePageChange}
            previousLabel={t`Previous`}
            nextLabel={t`Next`}
            trackingTitle="Users"
            className="w-full"
          />
        </div>
      )}
    </>
  );
}
