import type { RowKey } from "@repo/ui/components/Table";

import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { useUsers } from "@repo/infrastructure/sync/hooks";
import { Empty, EmptyDescription, EmptyHeader, EmptyMedia, EmptyTitle } from "@repo/ui/components/Empty";
import { Table, TableBody } from "@repo/ui/components/Table";
import { useViewportResize } from "@repo/ui/hooks/useViewportResize";
import { useNavigate, useSearch } from "@tanstack/react-router";
import { SearchIcon } from "lucide-react";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";

import { SortableUserProperties, SortOrder } from "@/shared/lib/api/sortTypes";

import { filterAndSortUsers } from "../-hooks/filterAndSortUsers";
import { type SortDescriptor, UserTableHeader } from "./UserTableHeader";
import { UserTableRow } from "./UserTableRow";

type ElectricUser = ReturnType<typeof useUsers>["data"][number];

interface UserTableProps {
  selectedUsers: ElectricUser[];
  onSelectedUsersChange: (users: ElectricUser[]) => void;
  onViewProfile: (user: ElectricUser | null) => void;
  onDeleteUser: (user: ElectricUser) => void;
  onChangeRole: (user: ElectricUser) => void;
  onUsersLoaded?: (users: ElectricUser[]) => void;
}

export function UserTable({
  selectedUsers,
  onSelectedUsersChange,
  onViewProfile,
  onDeleteUser,
  onChangeRole,
  onUsersLoaded
}: Readonly<UserTableProps>) {
  const isMobile = useViewportResize();
  const navigate = useNavigate();
  const { search, userRole, userStatus, startDate, endDate, orderBy, sortOrder, userId } = useSearch({ strict: false });
  const userInfo = useUserInfo();

  const { data: allUsers } = useUsers();

  const usersList = useMemo(
    () => filterAndSortUsers(allUsers, { search, userRole, userStatus, startDate, endDate, orderBy, sortOrder }),
    [allUsers, search, userRole, userStatus, startDate, endDate, orderBy, sortOrder]
  );

  const hasFilters = Boolean(search || userRole || userStatus || startDate || endDate);

  const [sortDescriptor, setSortDescriptor] = useState<SortDescriptor>(() => ({
    column: orderBy ?? SortableUserProperties.Name,
    direction: sortOrder === SortOrder.Descending ? "descending" : "ascending"
  }));

  const selectedKeys = useMemo<ReadonlySet<RowKey>>(
    () => new Set(selectedUsers.map((user) => user.id)),
    [selectedUsers]
  );

  const handleSelectionChange = useCallback(
    (keys: Set<RowKey>) => {
      onSelectedUsersChange(usersList.filter((user) => keys.has(user.id)));
      if (keys.size > 1) onViewProfile(null);
    },
    [onSelectedUsersChange, onViewProfile, usersList]
  );

  const handleActivate = useCallback(
    (key: RowKey) => {
      onViewProfile(userId === key ? null : (usersList.find((user) => user.id === key) ?? null));
    },
    [userId, onViewProfile, usersList]
  );

  const handleSortChange = useCallback(
    (columnId: string) => {
      const newDirection =
        sortDescriptor.column === columnId && sortDescriptor.direction === "ascending" ? "descending" : "ascending";
      setSortDescriptor({ column: columnId, direction: newDirection });
      onSelectedUsersChange([]);
      const newOrderBy = columnId as SortableUserProperties;
      const newSortOrder = newDirection === "ascending" ? SortOrder.Ascending : SortOrder.Descending;
      navigate({
        to: "/account/users",
        search: (prev) => ({
          ...prev,
          orderBy: newOrderBy === SortableUserProperties.Name ? undefined : newOrderBy,
          sortOrder: newSortOrder === SortOrder.Ascending ? undefined : newSortOrder,
          pageOffset: undefined
        })
      });
    },
    [navigate, sortDescriptor, onSelectedUsersChange]
  );

  const previousUserIds = useRef<string>("");
  useEffect(() => {
    const userIds = usersList.map((u) => u.id).join(",");
    if (userIds !== previousUserIds.current) {
      previousUserIds.current = userIds;
      onUsersLoaded?.(usersList);
    }
  }, [usersList, onUsersLoaded]);

  if (usersList.length === 0 && hasFilters) {
    return (
      <Empty>
        <EmptyHeader>
          <EmptyMedia variant="icon">
            <SearchIcon />
          </EmptyMedia>
          <EmptyTitle>
            <Trans>No users found</Trans>
          </EmptyTitle>
          <EmptyDescription>
            <Trans>Try adjusting your search or filters</Trans>
          </EmptyDescription>
        </EmptyHeader>
      </Empty>
    );
  }

  return (
    <div className="flex-1 overflow-visible rounded-md bg-background outline-ring focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 max-sm:pb-18 sm:min-h-48 sm:overflow-auto">
      <Table
        rowSize="compact"
        aria-label={t`Users`}
        selectionMode="multiple"
        selectedKeys={selectedKeys}
        onSelectionChange={handleSelectionChange}
        onActivate={handleActivate}
        activateOnNavigate={userId != null}
        scrollToKey={userId}
      >
        <UserTableHeader sortDescriptor={sortDescriptor} isMobile={isMobile} onSortChange={handleSortChange} />
        <TableBody>
          {usersList.map((user) => (
            <UserTableRow
              key={user.id}
              user={user}
              isMobile={isMobile}
              currentUserRole={userInfo?.role}
              currentUserId={userInfo?.id}
              onSelectedUsersChange={onSelectedUsersChange}
              onViewProfile={onViewProfile}
              onDeleteUser={onDeleteUser}
              onChangeRole={onChangeRole}
            />
          ))}
        </TableBody>
      </Table>
    </div>
  );
}
