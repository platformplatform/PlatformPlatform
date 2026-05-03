import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from "@repo/ui/components/Empty";
import { Field, FieldLabel } from "@repo/ui/components/Field";
import { InputGroup, InputGroupAddon, InputGroupButton, InputGroupInput } from "@repo/ui/components/InputGroup";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@repo/ui/components/Select";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { Table, TableBody, TableHead, TableHeader, TableRow } from "@repo/ui/components/Table";
import { TablePagination } from "@repo/ui/components/TablePagination";
import { useDebounce } from "@repo/ui/hooks/useDebounce";
import { useFormatDate } from "@repo/ui/hooks/useSmartDate";
import { keepPreviousData } from "@tanstack/react-query";
import { SearchIcon, XIcon } from "lucide-react";
import { useEffect, useState } from "react";

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
  const [role, setRole] = useState<UserRole | "">("");
  const [pageOffset, setPageOffset] = useState(0);
  const debouncedSearch = useDebounce(searchInput, 500);

  useEffect(() => {
    setPageOffset(0);
  }, [debouncedSearch, role]);

  const { data, isLoading } = api.useQuery(
    "get",
    "/api/back-office/tenants/{id}/users",
    {
      params: {
        path: { id: tenantId },
        query: {
          Search: debouncedSearch || undefined,
          Role: role || undefined,
          PageOffset: pageOffset || undefined
        }
      }
    },
    { placeholderData: keepPreviousData }
  );

  const users = data?.users ?? [];
  const totalPages = data?.totalPages ?? 0;
  const currentPage = (data?.currentPageOffset ?? 0) + 1;
  const hasFilters = Boolean(debouncedSearch || role);

  return (
    <div className="flex flex-col gap-4">
      <UserFilters searchInput={searchInput} onSearchChange={setSearchInput} role={role} onRoleChange={setRole} />
      <UserList users={users} isLoading={isLoading} hasFilters={hasFilters} />
      {totalPages > 1 && (
        <TablePagination
          currentPage={currentPage}
          totalPages={totalPages}
          onPageChange={(page) => setPageOffset(page - 1)}
          previousLabel={t`Previous`}
          nextLabel={t`Next`}
          trackingTitle="Tenant users"
          className="w-full"
        />
      )}
    </div>
  );
}

function UserFilters({
  searchInput,
  onSearchChange,
  role,
  onRoleChange
}: Readonly<{
  searchInput: string;
  onSearchChange: (value: string) => void;
  role: UserRole | "";
  onRoleChange: (value: UserRole | "") => void;
}>) {
  return (
    <div className="flex flex-wrap items-end gap-3">
      <Field className="min-w-[14rem] flex-1">
        <FieldLabel>{t`Search users`}</FieldLabel>
        <InputGroup>
          <InputGroupAddon>
            <SearchIcon />
          </InputGroupAddon>
          <InputGroupInput
            type="text"
            role="searchbox"
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
      </Field>

      <Field className="flex flex-col">
        <FieldLabel>
          <Trans>Role</Trans>
        </FieldLabel>
        <Select<string> value={role} onValueChange={(value) => onRoleChange((value as UserRole | "") || "")}>
          <SelectTrigger aria-label={t`Role`} className="min-w-[10rem]">
            <SelectValue>{(value: string) => (value ? getUserRoleLabel(value as UserRole) : t`Any role`)}</SelectValue>
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="">
              <Trans>Any role</Trans>
            </SelectItem>
            {Object.values(UserRole).map((value) => (
              <SelectItem key={value} value={value}>
                {getUserRoleLabel(value)}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </Field>
    </div>
  );
}

function UserList({
  users,
  isLoading,
  hasFilters
}: Readonly<{ users: TenantUserSummary[]; isLoading: boolean; hasFilters: boolean }>) {
  const formatDate = useFormatDate();

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
      <Empty className="border">
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
    <div className="overflow-hidden rounded-md border border-border">
      <Table rowSize="spacious" aria-label={t`Tenant users`}>
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
    </div>
  );
}
