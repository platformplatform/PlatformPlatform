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
  onViewProfile: (user: ElectricUser | null, isKeyboardOpen?: boolean) => void;
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
  const { search, userRole, userStatus, startDate, endDate, orderBy, sortOrder } = useSearch({ strict: false });
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
  const selectedUserIds = useMemo(() => new Set(selectedUsers.map((user) => user.id)), [selectedUsers]);

  const handleSortChange = useCallback(
    (columnId: string) => {
      const newDirection =
        sortDescriptor.column === columnId && sortDescriptor.direction === "ascending" ? "descending" : "ascending";

      const newSortDescriptor: SortDescriptor = {
        column: columnId,
        direction: newDirection
      };
      setSortDescriptor(newSortDescriptor);
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

  const handleRowClick = useCallback(
    (user: ElectricUser, event: React.MouseEvent) => {
      const target = event.target as HTMLElement;
      if (target.closest("button") || target.closest('[role="menuitem"]')) {
        return;
      }

      const clickedIndex = usersList.findIndex((u) => u.id === user.id);
      const isSelected = selectedUserIds.has(user.id);
      const isCtrlOrCmd = event.ctrlKey || event.metaKey;
      const isShift = event.shiftKey;

      if (isCtrlOrCmd) {
        if (isSelected) {
          const newSelection = selectedUsers.filter((u) => u.id !== user.id);
          onSelectedUsersChange(newSelection);
        } else {
          onSelectedUsersChange([...selectedUsers, user]);
        }
        onViewProfile(null);
      } else if (isShift && selectedUsers.length > 0) {
        const firstSelectedIndex = usersList.findIndex((u) => u.id === selectedUsers[0].id);
        const start = Math.min(firstSelectedIndex, clickedIndex);
        const end = Math.max(firstSelectedIndex, clickedIndex);
        const rangeUsers = usersList.slice(start, end + 1);
        onSelectedUsersChange(rangeUsers);
        onViewProfile(null);
      } else if (isSelected && selectedUsers.length === 1) {
        onSelectedUsersChange([]);
        onViewProfile(null);
      } else {
        onSelectedUsersChange([user]);
        onViewProfile(user, false);
      }
    },
    [usersList, selectedUserIds, selectedUsers, onSelectedUsersChange, onViewProfile]
  );

  const currentSelectedIndex =
    selectedUsers.length === 1 ? usersList.findIndex((u) => u.id === selectedUsers[0].id) : -1;

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
        aria-label={t`Users`}
        selectedIndex={currentSelectedIndex}
        onNavigate={(index) => onSelectedUsersChange([usersList[index]])}
        onActivate={(index) => onViewProfile(usersList[index], true)}
      >
        <UserTableHeader sortDescriptor={sortDescriptor} isMobile={isMobile} onSortChange={handleSortChange} />
        <TableBody>
          {usersList.map((user, index) => (
            <UserTableRow
              key={user.id}
              user={user}
              index={index}
              isSelected={selectedUserIds.has(user.id)}
              isMobile={isMobile}
              currentUserRole={userInfo?.role}
              currentUserId={userInfo?.id}
              onRowClick={handleRowClick}
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
