import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { useDeletedUsers } from "@repo/infrastructure/sync/hooks";
import { Avatar, AvatarFallback, AvatarImage } from "@repo/ui/components/Avatar";
import { Badge } from "@repo/ui/components/Badge";
import { Checkbox } from "@repo/ui/components/Checkbox";
import { Empty, EmptyDescription, EmptyHeader, EmptyMedia, EmptyTitle } from "@repo/ui/components/Empty";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@repo/ui/components/Table";
import { useViewportResize } from "@repo/ui/hooks/useViewportResize";
import { isMediumViewportOrLarger, isSmallViewportOrLarger, isTouchDevice } from "@repo/ui/utils/responsive";
import { getInitials } from "@repo/utils/string/getInitials";
import { Trash2Icon } from "lucide-react";
import { useCallback, useMemo } from "react";
import { SmartDate } from "@/shared/components/SmartDate";
import type { UserRole } from "@/shared/lib/api/client";
import { getUserRoleLabel } from "@/shared/lib/api/userRole";

type ElectricDeletedUser = ReturnType<typeof useDeletedUsers>["data"][number];

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
              <span className="font-bold text-xs">
                <Trans>Name</Trans>
              </span>
            </TableHead>
            {isSmallViewportOrLarger() && (
              <TableHead className="min-w-[10rem]">
                <span className="font-bold text-xs">
                  <Trans>Email</Trans>
                </span>
              </TableHead>
            )}
            {isMediumViewportOrLarger() && (
              <TableHead className="w-[9rem] min-w-[7.5rem]">
                <span className="font-bold text-xs">
                  <Trans>Deleted</Trans>
                </span>
              </TableHead>
            )}
            {isSmallViewportOrLarger() && (
              <TableHead className="w-[6rem]">
                <span className="font-bold text-xs">
                  <Trans>Role</Trans>
                </span>
              </TableHead>
            )}
          </TableRow>
        </TableHeader>
        <TableBody>
          {users.map((user) => {
            const isSelected = selectedUserIds.has(user.id);
            return (
              <TableRow
                key={user.id}
                data-state={isSelected ? "selected" : undefined}
                className={`cursor-pointer select-none ${isSelected ? "bg-active-background hover:bg-active-background" : "hover:bg-hover-background"}`}
                onClick={(event) => handleRowClick(user, event)}
              >
                {isMultiSelectMode && (
                  <TableCell>
                    <Checkbox
                      checked={isSelected}
                      onCheckedChange={() => handleSelectRow(user, true)}
                      aria-label={t`Select ${user.firstName ?? ""} ${user.lastName ?? ""}`}
                    />
                  </TableCell>
                )}
                <TableCell>
                  <div className="flex h-14 w-full items-center justify-between gap-2 p-0">
                    <div className="flex min-w-0 flex-1 items-center gap-2 text-left font-normal">
                      <Avatar size="lg">
                        <AvatarImage src={user.avatarUrl ?? undefined} />
                        <AvatarFallback>
                          {getInitials(user.firstName ?? undefined, user.lastName ?? undefined, user.email)}
                        </AvatarFallback>
                      </Avatar>
                      <div className="flex min-w-0 flex-1 flex-col">
                        <div className="flex items-center gap-2 truncate text-foreground">
                          <span className="truncate">
                            {user.firstName || user.lastName
                              ? `${user.firstName} ${user.lastName}`.trim()
                              : !isSmallViewportOrLarger()
                                ? user.email
                                : ""}
                          </span>
                        </div>
                        <span className="block truncate text-muted-foreground text-sm">{user.title ?? ""}</span>
                      </div>
                    </div>
                  </div>
                </TableCell>
                {isSmallViewportOrLarger() && (
                  <TableCell>
                    <span className="block h-full w-full justify-start p-0 text-left font-normal">{user.email}</span>
                  </TableCell>
                )}
                {isMediumViewportOrLarger() && (
                  <TableCell>
                    <SmartDate date={user.deletedAt} className="text-foreground" />
                  </TableCell>
                )}
                {isSmallViewportOrLarger() && (
                  <TableCell>
                    <Badge variant="outline">{getUserRoleLabel(user.role as UserRole)}</Badge>
                  </TableCell>
                )}
              </TableRow>
            );
          })}
        </TableBody>
      </Table>
    </div>
  );
}
