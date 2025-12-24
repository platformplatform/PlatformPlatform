import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { Avatar } from "@repo/ui/components/Avatar";
import { Badge } from "@repo/ui/components/Badge";
import { Button } from "@repo/ui/components/Button";
import { Checkbox } from "@repo/ui/components/Checkbox";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger
} from "@repo/ui/components/DropdownMenu";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@repo/ui/components/Table";
import { TablePagination } from "@repo/ui/components/TablePagination";
import { Text } from "@repo/ui/components/Text";
import { useInfiniteScroll } from "@repo/ui/hooks/useInfiniteScroll";
import { useKeyboardNavigation } from "@repo/ui/hooks/useKeyboardNavigation";
import { useViewportResize } from "@repo/ui/hooks/useViewportResize";
import { isMediumViewportOrLarger, isSmallViewportOrLarger, isTouchDevice } from "@repo/ui/utils/responsive";
import { getInitials } from "@repo/utils/string/getInitials";
import { useNavigate, useSearch } from "@tanstack/react-router";
import { ArrowUp, EllipsisVerticalIcon, SettingsIcon, Trash2Icon, UserIcon } from "lucide-react";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { SmartDate } from "@/shared/components/SmartDate";
import { api, type components, SortableUserProperties, SortOrder } from "@/shared/lib/api/client";
import { getUserRoleLabel } from "@/shared/lib/api/userRole";
import { useInfiniteUsers } from "../-hooks/useInfiniteUsers";

type UserDetails = components["schemas"]["UserDetails"];

type SortDescriptor = {
  column: string;
  direction: "ascending" | "descending";
};

interface UserTableProps {
  selectedUsers: UserDetails[];
  onSelectedUsersChange: (users: UserDetails[]) => void;
  onViewProfile: (user: UserDetails | null, isKeyboardOpen?: boolean) => void;
  onDeleteUser: (user: UserDetails) => void;
  onChangeRole: (user: UserDetails) => void;
  onUsersLoaded?: (users: UserDetails[]) => void;
}

export function UserTable({
  selectedUsers,
  onSelectedUsersChange,
  onViewProfile,
  onDeleteUser,
  onChangeRole,
  onUsersLoaded
}: Readonly<UserTableProps>) {
  const navigate = useNavigate();
  const { search, userRole, userStatus, startDate, endDate, orderBy, sortOrder, pageOffset } = useSearch({
    strict: false
  });
  const userInfo = useUserInfo();
  const tableContainerRef = useRef<HTMLDivElement>(null);

  const [sortDescriptor, setSortDescriptor] = useState<SortDescriptor>(() => ({
    column: orderBy ?? SortableUserProperties.Name,
    direction: sortOrder === SortOrder.Descending ? "descending" : "ascending"
  }));
  const isKeyboardNavigation = useKeyboardNavigation();
  const isMobile = useViewportResize();
  const [focusedRowIndex, setFocusedRowIndex] = useState<number>(-1);

  const selectedUserIds = useMemo(() => new Set(selectedUsers.map((user) => user.id)), [selectedUsers]);
  const isMultiSelectMode = !isTouchDevice() && isMediumViewportOrLarger();

  // Use regular query for desktop
  const { data: desktopUsers, isLoading: isDesktopLoading } = api.useQuery("get", "/api/account-management/users", {
    params: {
      query: {
        Search: search,
        UserRole: userRole,
        UserStatus: userStatus,
        StartDate: startDate,
        EndDate: endDate,
        OrderBy: orderBy ?? SortableUserProperties.Name,
        SortOrder: sortOrder ?? SortOrder.Ascending,
        PageOffset: pageOffset ?? 0
      }
    },
    enabled: !isMobile
  });

  // Use infinite scroll for mobile
  const {
    users: mobileUsers,
    isLoading: isMobileLoading,
    isLoadingMore,
    hasMore,
    loadMore
  } = useInfiniteUsers({
    search,
    userRole,
    userStatus,
    startDate,
    endDate,
    orderBy,
    sortOrder,
    enabled: isMobile
  });

  // Select data based on device
  const users = isMobile ? { users: mobileUsers, totalPages: 1, currentPageOffset: 0 } : desktopUsers;
  const isLoading = isMobile ? isMobileLoading : isDesktopLoading;

  const handlePageChange = useCallback(
    (page: number) => {
      navigate({
        to: "/admin/users",
        search: (prev) => ({
          ...prev,
          pageOffset: page === 1 ? undefined : page - 1
        })
      });
    },
    [navigate]
  );

  const handleSortChange = useCallback(
    (columnId: string) => {
      const newDirection =
        sortDescriptor.column === columnId && sortDescriptor.direction === "ascending" ? "descending" : "ascending";

      const newSortDescriptor: SortDescriptor = {
        column: columnId,
        direction: newDirection
      };
      setSortDescriptor(newSortDescriptor);

      const newOrderBy = columnId as SortableUserProperties;
      const newSortOrder = newDirection === "ascending" ? SortOrder.Ascending : SortOrder.Descending;

      navigate({
        to: "/admin/users",
        search: (prev) => ({
          ...prev,
          orderBy: newOrderBy === SortableUserProperties.Name ? undefined : newOrderBy,
          sortOrder: newSortOrder === SortOrder.Ascending ? undefined : newSortOrder,
          pageOffset: undefined
        })
      });
    },
    [navigate, sortDescriptor]
  );

  useEffect(() => {
    onSelectedUsersChange([]);
  }, [onSelectedUsersChange, pageOffset]);

  useEffect(() => {
    if (users?.users && onUsersLoaded) {
      onUsersLoaded(users.users);
    }
  }, [users?.users, onUsersLoaded]);

  const handleSelectAll = useCallback(
    (checked: boolean) => {
      if (checked) {
        onSelectedUsersChange(users?.users ?? []);
        onViewProfile(null);
      } else {
        onSelectedUsersChange([]);
        onViewProfile(null);
      }
    },
    [users?.users, onSelectedUsersChange, onViewProfile]
  );

  const handleSelectRow = useCallback(
    (user: UserDetails, isCheckboxClick: boolean) => {
      if (isMultiSelectMode && isCheckboxClick) {
        const isSelected = selectedUserIds.has(user.id);
        if (isSelected) {
          const newSelection = selectedUsers.filter((u) => u.id !== user.id);
          onSelectedUsersChange(newSelection);
          if (newSelection.length === 1) {
            onViewProfile(newSelection[0], false);
          } else {
            onViewProfile(null);
          }
        } else {
          const newSelection = [...selectedUsers, user];
          onSelectedUsersChange(newSelection);
          if (newSelection.length === 1) {
            onViewProfile(newSelection[0], false);
          } else {
            onViewProfile(null);
          }
        }
      } else {
        onSelectedUsersChange([user]);
        if (!isKeyboardNavigation) {
          onViewProfile(user, false);
        }
      }
    },
    [isMultiSelectMode, selectedUserIds, selectedUsers, onSelectedUsersChange, onViewProfile, isKeyboardNavigation]
  );

  const handleRowClick = useCallback(
    (user: UserDetails, event: React.MouseEvent) => {
      const target = event.target as HTMLElement;
      if (target.closest("button") || target.closest('[data-slot="checkbox"]') || target.closest('[role="menuitem"]')) {
        return;
      }
      handleSelectRow(user, false);
    },
    [handleSelectRow]
  );

  // Handle keyboard navigation
  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if (!users?.users.length) {
        return;
      }

      const activeElement = document.activeElement;
      const tableContainer = tableContainerRef.current;
      if (!tableContainer?.contains(activeElement)) {
        return;
      }

      const target = event.target as HTMLElement;
      if (target.tagName === "BUTTON" || target.closest("button")) {
        return;
      }

      if (event.key === "Enter" && selectedUsers.length === 1) {
        event.preventDefault();
        event.stopPropagation();
        onViewProfile(selectedUsers[0], true);
        return;
      }

      if (event.key === "ArrowDown") {
        event.preventDefault();
        const nextIndex = focusedRowIndex < users.users.length - 1 ? focusedRowIndex + 1 : 0;
        setFocusedRowIndex(nextIndex);
        handleSelectRow(users.users[nextIndex], false);
      } else if (event.key === "ArrowUp") {
        event.preventDefault();
        const prevIndex = focusedRowIndex > 0 ? focusedRowIndex - 1 : users.users.length - 1;
        setFocusedRowIndex(prevIndex);
        handleSelectRow(users.users[prevIndex], false);
      }
    };

    document.addEventListener("keydown", handleKeyDown, true);
    return () => {
      document.removeEventListener("keydown", handleKeyDown, true);
    };
  }, [selectedUsers, onViewProfile, users?.users, focusedRowIndex, handleSelectRow]);

  // Use infinite scroll hook for mobile
  const loadMoreRef = useInfiniteScroll({
    enabled: isMobile,
    hasMore,
    isLoadingMore,
    onLoadMore: loadMore
  });

  if (isLoading) {
    return null;
  }

  const currentPage = users ? users.currentPageOffset + 1 : 1;
  const usersLength = users?.users.length ?? 0;
  const allSelected = usersLength > 0 && selectedUserIds.size === usersLength;
  const someSelected = selectedUserIds.size > 0 && selectedUserIds.size < usersLength;

  return (
    <>
      <div ref={tableContainerRef} className="min-h-48 flex-1 overflow-auto">
        <Table aria-label={t`Users`}>
          <TableHeader className="sticky top-0 z-10 bg-background">
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
              <TableHead
                className={`cursor-pointer select-none ${isSmallViewportOrLarger() ? "min-w-[250px]" : ""}`}
                onClick={() => handleSortChange(SortableUserProperties.Name)}
              >
                <div className="flex items-center gap-1 font-bold text-xs">
                  <span>
                    <Trans>Name</Trans>
                  </span>
                  <SortIndicator sortDescriptor={sortDescriptor} columnId={SortableUserProperties.Name} />
                </div>
              </TableHead>
              {isSmallViewportOrLarger() && (
                <TableHead
                  className="min-w-[160px] cursor-pointer select-none"
                  onClick={() => handleSortChange(SortableUserProperties.Email)}
                >
                  <div className="flex items-center gap-1 font-bold text-xs">
                    <span>
                      <Trans>Email</Trans>
                    </span>
                    <SortIndicator sortDescriptor={sortDescriptor} columnId={SortableUserProperties.Email} />
                  </div>
                </TableHead>
              )}
              {isMediumViewportOrLarger() && (
                <TableHead
                  className="w-[110px] min-w-[65px] cursor-pointer select-none"
                  onClick={() => handleSortChange(SortableUserProperties.CreatedAt)}
                >
                  <div className="flex items-center gap-1 font-bold text-xs">
                    <span>
                      <Trans>Created</Trans>
                    </span>
                    <SortIndicator sortDescriptor={sortDescriptor} columnId={SortableUserProperties.CreatedAt} />
                  </div>
                </TableHead>
              )}
              {isMediumViewportOrLarger() && (
                <TableHead
                  className="w-[120px] min-w-[65px] cursor-pointer select-none"
                  onClick={() => handleSortChange(SortableUserProperties.ModifiedAt)}
                >
                  <div className="flex items-center gap-1 font-bold text-xs">
                    <span>
                      <Trans>Modified</Trans>
                    </span>
                    <SortIndicator sortDescriptor={sortDescriptor} columnId={SortableUserProperties.ModifiedAt} />
                  </div>
                </TableHead>
              )}
              {isSmallViewportOrLarger() && (
                <TableHead
                  className="w-[135px] cursor-pointer select-none"
                  onClick={() => handleSortChange(SortableUserProperties.Role)}
                >
                  <div className="flex items-center gap-1 font-bold text-xs">
                    <span>
                      <Trans>Role</Trans>
                    </span>
                    <SortIndicator sortDescriptor={sortDescriptor} columnId={SortableUserProperties.Role} />
                  </div>
                </TableHead>
              )}
            </TableRow>
          </TableHeader>
          <TableBody>
            {users?.users.map((user, index) => {
              const isSelected = selectedUserIds.has(user.id);
              return (
                <TableRow
                  key={user.id}
                  data-state={isSelected ? "selected" : undefined}
                  className={`cursor-pointer select-none ${isSelected ? "bg-active-background hover:bg-active-background" : "hover:bg-hover-background"}`}
                  onClick={(event) => handleRowClick(user, event)}
                  onFocus={() => setFocusedRowIndex(index)}
                  tabIndex={index === focusedRowIndex ? 0 : -1}
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
                        <Avatar
                          initials={getInitials(user.firstName, user.lastName, user.email)}
                          avatarUrl={user.avatarUrl}
                          size="sm"
                          isRound={true}
                        />
                        <div className="flex min-w-0 flex-1 flex-col">
                          <div className="flex items-center gap-2 truncate text-foreground">
                            <span className="truncate">
                              {user.firstName || user.lastName
                                ? `${user.firstName} ${user.lastName}`.trim()
                                : !isSmallViewportOrLarger()
                                  ? user.email
                                  : ""}
                            </span>
                            {user.emailConfirmed ? null : (
                              <Badge variant="outline" className="shrink-0">
                                <Trans>Pending</Trans>
                              </Badge>
                            )}
                          </div>
                          <Text className="truncate text-muted-foreground text-sm">{user.title ?? ""}</Text>
                        </div>
                      </div>
                    </div>
                  </TableCell>
                  {isSmallViewportOrLarger() && (
                    <TableCell>
                      <Text className="h-full w-full justify-start p-0 text-left font-normal">{user.email}</Text>
                    </TableCell>
                  )}
                  {isMediumViewportOrLarger() && (
                    <TableCell>
                      <SmartDate date={user.createdAt} className="text-foreground" />
                    </TableCell>
                  )}
                  {isMediumViewportOrLarger() && (
                    <TableCell>
                      <SmartDate date={user.modifiedAt} className="text-foreground" />
                    </TableCell>
                  )}
                  {isSmallViewportOrLarger() && (
                    <TableCell>
                      <div className="flex h-full w-full items-center justify-between p-0">
                        <Badge variant="outline">{getUserRoleLabel(user.role)}</Badge>
                        <DropdownMenu
                          onOpenChange={(isOpen) => {
                            if (isOpen) {
                              onSelectedUsersChange([user]);
                            }
                          }}
                        >
                          <DropdownMenuTrigger
                            render={
                              <Button variant="ghost" size="icon" aria-label={t`User actions`}>
                                <EllipsisVerticalIcon className="h-5 w-5 text-muted-foreground" />
                              </Button>
                            }
                          />
                          <DropdownMenuContent>
                            <DropdownMenuItem onClick={() => onViewProfile(user, false)}>
                              <UserIcon className="h-4 w-4" />
                              <Trans>View profile</Trans>
                            </DropdownMenuItem>
                            {userInfo?.role === "Owner" && (
                              <>
                                <DropdownMenuItem
                                  disabled={user.id === userInfo?.id}
                                  onClick={() => onChangeRole(user)}
                                >
                                  <SettingsIcon className="h-4 w-4" />
                                  <Trans>Change role</Trans>
                                </DropdownMenuItem>
                                <DropdownMenuSeparator />
                                <DropdownMenuItem
                                  disabled={user.id === userInfo?.id}
                                  variant="destructive"
                                  onClick={() => onDeleteUser(user)}
                                >
                                  <Trash2Icon className="h-4 w-4" />
                                  <Trans>Delete</Trans>
                                </DropdownMenuItem>
                              </>
                            )}
                          </DropdownMenuContent>
                        </DropdownMenu>
                      </div>
                    </TableCell>
                  )}
                </TableRow>
              );
            })}
          </TableBody>
        </Table>
      </div>

      {/* Mobile: Loading indicator for infinite scroll */}
      {isMobile && <div ref={loadMoreRef} className="h-1" />}

      {/* Desktop: Regular pagination */}
      {!isMobile && users && (
        <div className="flex-shrink-0 bg-background pt-4">
          <TablePagination
            paginationSize={9}
            currentPage={currentPage}
            totalPages={users.totalPages ?? 1}
            onPageChange={handlePageChange}
            previousLabel={t`Previous`}
            nextLabel={t`Next`}
            className="w-full"
          />
        </div>
      )}
    </>
  );
}

interface SortIndicatorProps {
  sortDescriptor: SortDescriptor;
  columnId: string;
}

function SortIndicator({ sortDescriptor, columnId }: Readonly<SortIndicatorProps>) {
  if (sortDescriptor.column !== columnId) {
    return null;
  }

  return (
    <span
      className={`flex h-4 w-4 items-center justify-center transition ${sortDescriptor.direction === "descending" ? "rotate-180" : ""}`}
    >
      <ArrowUp aria-hidden={true} className="h-4 w-4 text-muted-foreground" />
    </span>
  );
}
