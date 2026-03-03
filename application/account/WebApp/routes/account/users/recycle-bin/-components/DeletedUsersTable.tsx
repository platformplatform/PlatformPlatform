import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { useDeletedUsers } from "@repo/infrastructure/sync/hooks";
import { Checkbox } from "@repo/ui/components/Checkbox";
import { Empty, EmptyDescription, EmptyHeader, EmptyMedia, EmptyTitle } from "@repo/ui/components/Empty";
import { Table, TableBody, TableHead, TableHeader, TableRow } from "@repo/ui/components/Table";
import { useViewportResize } from "@repo/ui/hooks/useViewportResize";
import { isMediumViewportOrLarger, isSmallViewportOrLarger, isTouchDevice } from "@repo/ui/utils/responsive";
import { Trash2Icon } from "lucide-react";
import { useCallback, useMemo } from "react";

import { DeletedUserRow } from "./DeletedUserRow";

type ElectricDeletedUser = ReturnType<typeof useDeletedUsers>["data"][number];

function DeletedUsersEmptyState() {
  return (
    <Empty>
      <EmptyHeader>
        <EmptyMedia variant="icon">
          <Trash2Icon />
        </EmptyMedia>
        <EmptyTitle>
          <Trans>Recycle bin is empty</Trans>
        </EmptyTitle>
        <EmptyDescription>
          <Trans>Deleted users will appear here for recovery</Trans>
        </EmptyDescription>
      </EmptyHeader>
    </Empty>
  );
}

interface DeletedUsersTableProps {
  selectedUsers: ElectricDeletedUser[];
  onSelectedUsersChange: (users: ElectricDeletedUser[]) => void;
}

export function DeletedUsersTable({ selectedUsers, onSelectedUsersChange }: Readonly<DeletedUsersTableProps>) {
  const _isMobile = useViewportResize();
  const { data: users } = useDeletedUsers();

  const selectedUserIds = useMemo(() => new Set(selectedUsers.map((user) => user.id)), [selectedUsers]);
  const isMultiSelectMode = !isTouchDevice() && isMediumViewportOrLarger();

  const handleSelectAll = useCallback(
    (checked: boolean) => {
      if (checked) {
        onSelectedUsersChange(users);
      } else {
        onSelectedUsersChange([]);
      }
    },
    [users, onSelectedUsersChange]
  );

  const handleSelectRow = useCallback(
    (user: ElectricDeletedUser, isCheckboxClick: boolean) => {
      if (isMultiSelectMode && isCheckboxClick) {
        const isSelected = selectedUserIds.has(user.id);
        if (isSelected) {
          onSelectedUsersChange(selectedUsers.filter((u) => u.id !== user.id));
        } else {
          onSelectedUsersChange([...selectedUsers, user]);
        }
      } else {
        onSelectedUsersChange([user]);
      }
    },
    [isMultiSelectMode, selectedUserIds, selectedUsers, onSelectedUsersChange]
  );

  const handleRowClick = useCallback(
    (user: ElectricDeletedUser, event: React.MouseEvent) => {
      const target = event.target as HTMLElement;
      if (target.closest("button") || target.closest('[data-slot="checkbox"]')) {
        return;
      }
      handleSelectRow(user, false);
    },
    [handleSelectRow]
  );

  if (users.length === 0) {
    return <DeletedUsersEmptyState />;
  }

  const usersLength = users.length;
  const allSelected = usersLength > 0 && selectedUserIds.size === usersLength;
  const someSelected = selectedUserIds.size > 0 && selectedUserIds.size < usersLength;

  return (
    <div className="deleted-users-table min-h-48 flex-1 overflow-auto">
      <Table aria-label={t`Deleted users`}>
        <TableHeader className="sticky top-0 z-10 bg-inherit">
          <TableRow>
            {isMultiSelectMode && (
              <TableHead className="w-[3.5rem]">
                <Checkbox
                  checked={allSelected}
                  indeterminate={someSelected}
                  onCheckedChange={handleSelectAll}
                  aria-label={t`Select all users`}
                />
              </TableHead>
            )}
            <TableHead className={isSmallViewportOrLarger() ? "min-w-[16rem]" : ""}>
              <span className="text-xs font-bold">
                <Trans>Name</Trans>
              </span>
            </TableHead>
            {isSmallViewportOrLarger() && (
              <TableHead className="min-w-[10rem]">
                <span className="text-xs font-bold">
                  <Trans>Email</Trans>
                </span>
              </TableHead>
            )}
            {isMediumViewportOrLarger() && (
              <TableHead className="w-[9rem] min-w-[7.5rem]">
                <span className="text-xs font-bold">
                  <Trans>Deleted</Trans>
                </span>
              </TableHead>
            )}
            {isSmallViewportOrLarger() && (
              <TableHead className="w-[6rem]">
                <span className="text-xs font-bold">
                  <Trans>Role</Trans>
                </span>
              </TableHead>
            )}
          </TableRow>
        </TableHeader>
        <TableBody>
          {users.map((user) => (
            <DeletedUserRow
              key={user.id}
              user={user}
              isSelected={selectedUserIds.has(user.id)}
              isMultiSelectMode={isMultiSelectMode}
              onSelectRow={handleSelectRow}
              onRowClick={handleRowClick}
            />
          ))}
        </TableBody>
      </Table>
    </div>
  );
}
