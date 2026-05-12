import type { RowKey } from "@repo/ui/components/Table";

import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from "@repo/ui/components/Empty";
import { InputGroup, InputGroupAddon, InputGroupButton, InputGroupInput } from "@repo/ui/components/InputGroup";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { Table, TableBody, TableHead, TableHeader, TableRow } from "@repo/ui/components/Table";
import { TablePagination } from "@repo/ui/components/TablePagination";
import { ToggleGroup, ToggleGroupItem } from "@repo/ui/components/ToggleGroup";
import { useDebounce } from "@repo/ui/hooks/useDebounce";
import { useFormatDate } from "@repo/ui/hooks/useSmartDate";
import { keepPreviousData } from "@tanstack/react-query";
import { useNavigate } from "@tanstack/react-router";
import { SearchIcon, XIcon } from "lucide-react";
import { useCallback, useEffect, useState } from "react";

import type { components } from "@/shared/lib/api/client";

import { api, UserRole } from "@/shared/lib/api/client";
import { getUserRoleLabel } from "@/shared/lib/api/labels";

import { AccountUserRow } from "./AccountUserRow";

type TenantUserSummary = components["schemas"]["TenantUserSummary"];

interface AccountUsersTabProps {
  tenantId: string;
}

export function AccountUsersTab({ tenantId }: Readonly<AccountUsersTabProps>) {
  const [searchInput, setSearchInput] = useState("");
  const [roles, setRoles] = useState<UserRole[]>([]);
  const [pageOffset, setPageOffset] = useState(0);
  const debouncedSearch = useDebounce(searchInput, 500);

  useEffect(() => {
    setPageOffset(0);
  }, [debouncedSearch, roles]);

  const { data, isLoading } = api.useQuery(
    "get",
    "/api/back-office/tenants/{id}/users",
    {
      params: {
        path: { id: tenantId },
        query: {
          Search: debouncedSearch || undefined,
          Roles: roles.length === 0 ? undefined : roles,
          PageOffset: pageOffset || undefined
        }
      }
    },
    { placeholderData: keepPreviousData }
  );

  const users = data?.users ?? [];
  const totalPages = data?.totalPages ?? 0;
  const currentPage = (data?.currentPageOffset ?? 0) + 1;
  const hasFilters = Boolean(debouncedSearch) || roles.length > 0;

  return (
    <div className="flex flex-col gap-4">
      <UserFilters searchInput={searchInput} onSearchChange={setSearchInput} roles={roles} onRolesChange={setRoles} />
      <UserList users={users} isLoading={isLoading} hasFilters={hasFilters} />
      {totalPages > 1 && (
        <TablePagination
          currentPage={currentPage}
          totalPages={totalPages}
          onPageChange={(page) => setPageOffset(page - 1)}
          previousLabel={t`Previous`}
          nextLabel={t`Next`}
          trackingTitle="Account users"
          className="w-full"
        />
      )}
    </div>
  );
}

function UserFilters({
  searchInput,
  onSearchChange,
  roles,
  onRolesChange
}: Readonly<{
  searchInput: string;
  onSearchChange: (value: string) => void;
  roles: UserRole[];
  onRolesChange: (value: UserRole[]) => void;
}>) {
  const handleRolesChange = (values: string[]) => {
    onRolesChange(values as UserRole[]);
  };

  return (
    <div className="flex flex-wrap items-center gap-3">
      <div className="max-w-[20rem] min-w-[14rem] flex-1">
        <InputGroup>
          <InputGroupAddon>
            <SearchIcon />
          </InputGroupAddon>
          <InputGroupInput
            type="text"
            role="searchbox"
            aria-label={t`Search users`}
            placeholder={t`Search by name or email`}
            value={searchInput}
            onChange={(event) => onSearchChange(event.target.value)}
            onKeyDown={(event) => event.key === "Escape" && searchInput && onSearchChange("")}
          />
          {searchInput && (
            <InputGroupAddon align="inline-end">
              <InputGroupButton onClick={() => onSearchChange("")} size="icon-xs" aria-label={t`Clear search`}>
                <XIcon />
              </InputGroupButton>
            </InputGroupAddon>
          )}
        </InputGroup>
      </div>

      <ToggleGroup
        variant="outline"
        aria-label={t`Role`}
        multiple={true}
        value={roles}
        onValueChange={handleRolesChange}
      >
        {[UserRole.Owner, UserRole.Admin, UserRole.Member].map((value) => (
          <ToggleGroupItem key={value} value={value} className="min-w-[5rem] justify-center">
            {getUserRoleLabel(value)}
          </ToggleGroupItem>
        ))}
      </ToggleGroup>
    </div>
  );
}

function UserList({
  users,
  isLoading,
  hasFilters
}: Readonly<{ users: TenantUserSummary[]; isLoading: boolean; hasFilters: boolean }>) {
  const formatDate = useFormatDate();
  const navigate = useNavigate();
  const handleActivate = useCallback(
    (key: RowKey) => {
      const user = users.find((entry) => entry.id === key);
      if (!user) return;
      navigate({ to: "/users/$userId", params: { userId: user.id } });
    },
    [navigate, users]
  );

  if (isLoading && users.length === 0) {
    return (
      <div className="flex flex-col gap-2">
        {Array.from({ length: 6 }).map((_, index) => (
          <Skeleton key={`user-skeleton-${index}`} className="h-12 w-full" />
        ))}
      </div>
    );
  }

  if (users.length === 0) {
    return (
      <Empty className="border bg-card">
        <EmptyHeader>
          <EmptyTitle>{hasFilters ? <Trans>No matching users</Trans> : <Trans>No users</Trans>}</EmptyTitle>
          <EmptyDescription>
            {hasFilters ? <Trans>No users match your filters.</Trans> : <Trans>This account has no users.</Trans>}
          </EmptyDescription>
        </EmptyHeader>
      </Empty>
    );
  }

  return (
    <Table rowSize="spacious" aria-label={t`Account users`} selectionMode="single" onActivate={handleActivate}>
      <TableHeader>
        <TableRow>
          <TableHead>
            <Trans>Name</Trans>
          </TableHead>
          <TableHead className="hidden md:table-cell">
            <Trans>Email</Trans>
          </TableHead>
          <TableHead className="hidden lg:table-cell">
            <Trans>Role</Trans>
          </TableHead>
          <TableHead className="hidden lg:table-cell">
            <Trans>Last seen</Trans>
          </TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {users.map((user) => (
          <AccountUserRow key={user.id} user={user} formatDate={formatDate} />
        ))}
      </TableBody>
    </Table>
  );
}
