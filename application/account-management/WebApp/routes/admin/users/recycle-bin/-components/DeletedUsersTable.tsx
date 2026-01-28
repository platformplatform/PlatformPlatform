import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Avatar, AvatarFallback, AvatarImage } from "@repo/ui/components/Avatar";
import { Badge } from "@repo/ui/components/Badge";
import { Checkbox } from "@repo/ui/components/Checkbox";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@repo/ui/components/Table";
import { TablePagination } from "@repo/ui/components/TablePagination";
import { useViewportResize } from "@repo/ui/hooks/useViewportResize";
import { isMediumViewportOrLarger, isSmallViewportOrLarger, isTouchDevice } from "@repo/ui/utils/responsive";
import { getInitials } from "@repo/utils/string/getInitials";
import { useCallback, useMemo } from "react";
import { SmartDate } from "@/shared/components/SmartDate";
import { api, type components } from "@/shared/lib/api/client";
import { getUserRoleLabel } from "@/shared/lib/api/userRole";

type DeletedUserDetails = components["schemas"]["DeletedUserDetails"];

interface DeletedUsersTableProps {
  selectedUsers: DeletedUserDetails[];
  onSelectedUsersChange: (users: DeletedUserDetails[]) => void;
  pageOffset: number;
  onPageChange: (page: number) => void;
}

export function DeletedUsersTable({
  selectedUsers,
  onSelectedUsersChange,
  pageOffset,
  onPageChange
}: Readonly<DeletedUsersTableProps>) {
  const isMobile = useViewportResize();

  const { data: deletedUsersData, isLoading } = api.useQuery("get", "/api/account-management/users/deleted", {
    params: {
      query: {
        PageOffset: pageOffset,
        PageSize: 25
      }
    }
  });

  const selectedUserIds = useMemo(() => new Set(selectedUsers.map((user) => user.id)), [selectedUsers]);
  const isMultiSelectMode = !isTouchDevice() && isMediumViewportOrLarger();

  const handleSelectAll = useCallback(
    (checked: boolean) => {
      if (checked) {
        onSelectedUsersChange(deletedUsersData?.users ?? []);
      } else {
        onSelectedUsersChange([]);
      }
    },
    [deletedUsersData?.users, onSelectedUsersChange]
  );

  const handleSelectRow = useCallback(
    (user: DeletedUserDetails, isCheckboxClick: boolean) => {
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
    (user: DeletedUserDetails, event: React.MouseEvent) => {
      const target = event.target as HTMLElement;
      if (target.closest("button") || target.closest('[data-slot="checkbox"]')) {
        return;
      }
      handleSelectRow(user, false);
    },
    [handleSelectRow]
  );

  if (isLoading) {
    return null;
  }

  const users = deletedUsersData?.users ?? [];
  const currentPage = (deletedUsersData?.currentPageOffset ?? 0) + 1;
  const totalPages = deletedUsersData?.totalPages ?? 1;

  if (users.length === 0) {
    return (
      <div className="flex flex-1 items-center justify-center py-16">
        <p className="text-muted-foreground">
          <Trans>No deleted users</Trans>
        </p>
      </div>
    );
  }

  const usersLength = users.length;
  const allSelected = usersLength > 0 && selectedUserIds.size === usersLength;
  const someSelected = selectedUserIds.size > 0 && selectedUserIds.size < usersLength;

  return (
    <>
      <div className="deleted-users-table min-h-48 flex-1 overflow-auto">
        <Table aria-label={t`Deleted users`}>
          <TableHeader className="sticky top-0 z-10 bg-inherit">
            <TableRow>
              {isMultiSelectMode && (
                <TableHead className="w-[52px]">
                  <Checkbox
                    checked={allSelected}
                    indeterminate={someSelected}
                    onCheckedChange={handleSelectAll}
                    aria-label={t`Select all users`}
                  />
                </TableHead>
              )}
              <TableHead className={isSmallViewportOrLarger() ? "min-w-[250px]" : ""}>
                <span className="font-bold text-xs">
                  <Trans>Name</Trans>
                </span>
              </TableHead>
              {isSmallViewportOrLarger() && (
                <TableHead className="min-w-[160px]">
                  <span className="font-bold text-xs">
                    <Trans>Email</Trans>
                  </span>
                </TableHead>
              )}
              {isMediumViewportOrLarger() && (
                <TableHead className="w-[140px] min-w-[120px]">
                  <span className="font-bold text-xs">
                    <Trans>Deleted</Trans>
                  </span>
                </TableHead>
              )}
              {isSmallViewportOrLarger() && (
                <TableHead className="w-[100px]">
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
                        aria-label={t`Select ${user.firstName} ${user.lastName}`}
                      />
                    </TableCell>
                  )}
                  <TableCell>
                    <div className="flex h-14 w-full items-center justify-between gap-2 p-0">
                      <div className="flex min-w-0 flex-1 items-center gap-2 text-left font-normal">
                        <Avatar size="lg">
                          <AvatarImage src={user.avatarUrl ?? undefined} />
                          <AvatarFallback>{getInitials(user.firstName, user.lastName, user.email)}</AvatarFallback>
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
                      <Badge variant="outline">{getUserRoleLabel(user.role)}</Badge>
                    </TableCell>
                  )}
                </TableRow>
              );
            })}
          </TableBody>
        </Table>
      </div>

      {!isMobile && totalPages > 1 && (
        <div className="flex-shrink-0 pt-4">
          <TablePagination
            currentPage={currentPage}
            totalPages={totalPages}
            onPageChange={onPageChange}
            previousLabel={t`Previous`}
            nextLabel={t`Next`}
            className="w-full"
          />
        </div>
      )}
    </>
  );
}
