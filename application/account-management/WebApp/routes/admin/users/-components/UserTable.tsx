import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { Avatar } from "@repo/ui/components/Avatar";
import { Badge } from "@repo/ui/components/Badge";
import { Button } from "@repo/ui/components/Button";
import { Menu, MenuItem, MenuSeparator } from "@repo/ui/components/Menu";
import { Pagination } from "@repo/ui/components/Pagination";
import { Cell, Column, Row, Table, TableHeader } from "@repo/ui/components/Table";
import { Text } from "@repo/ui/components/Text";
import { useInfiniteScroll } from "@repo/ui/hooks/useInfiniteScroll";
import { useKeyboardNavigation } from "@repo/ui/hooks/useKeyboardNavigation";
import { useViewportResize } from "@repo/ui/hooks/useViewportResize";
import { isMediumViewportOrLarger, isSmallViewportOrLarger, isTouchDevice } from "@repo/ui/utils/responsive";
import { getInitials } from "@repo/utils/string/getInitials";
import { useNavigate, useSearch } from "@tanstack/react-router";
import { EllipsisVerticalIcon, SettingsIcon, Trash2Icon, UserIcon } from "lucide-react";
import { useCallback, useEffect, useState } from "react";
import type { Selection, SortDescriptor } from "react-aria-components";
import { MenuTrigger, TableBody } from "react-aria-components";
import { SmartDate } from "@/shared/components/SmartDate";
import { api, type components, SortableUserProperties, SortOrder } from "@/shared/lib/api/client";
import { getUserRoleLabel } from "@/shared/lib/api/userRole";
import { useInfiniteUsers } from "../-hooks/useInfiniteUsers";

type UserDetails = components["schemas"]["UserDetails"];

interface UserTableProps {
  selectedUsers: UserDetails[];
  onSelectedUsersChange: (users: UserDetails[]) => void;
  onViewProfile: (user: UserDetails | null, isKeyboardOpen?: boolean) => void;
  onDeleteUser: (user: UserDetails) => void;
  onChangeRole: (user: UserDetails) => void;
  onUsersLoaded?: (users: UserDetails[]) => void;
  isProfileOpen?: boolean;
}

export function UserTable({
  selectedUsers,
  onSelectedUsersChange,
  onViewProfile,
  onDeleteUser,
  onChangeRole,
  onUsersLoaded,
  isProfileOpen
}: Readonly<UserTableProps>) {
  const navigate = useNavigate();
  const { search, userRole, userStatus, startDate, endDate, orderBy, sortOrder, pageOffset } = useSearch({
    strict: false
  });
  const userInfo = useUserInfo();

  const [sortDescriptor, setSortDescriptor] = useState<SortDescriptor>(() => ({
    column: orderBy ?? SortableUserProperties.Name,
    direction: sortOrder === SortOrder.Descending ? "descending" : "ascending"
  }));
  const isKeyboardNavigation = useKeyboardNavigation();
  const isMobile = useViewportResize();

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
    (newSortDescriptor: SortDescriptor) => {
      setSortDescriptor(newSortDescriptor);
      const newOrderBy = (newSortDescriptor.column?.toString() ??
        SortableUserProperties.Name) as SortableUserProperties;
      const newSortOrder = newSortDescriptor.direction === "ascending" ? SortOrder.Ascending : SortOrder.Descending;

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
    [navigate]
  );

  useEffect(() => {
    onSelectedUsersChange([]);
  }, [onSelectedUsersChange, pageOffset]);

  useEffect(() => {
    if (users?.users && onUsersLoaded) {
      onUsersLoaded(users.users);
    }
  }, [users?.users, onUsersLoaded]);

  const handleSelectionChange = useCallback(
    (keys: Selection) => {
      if (keys === "all") {
        onSelectedUsersChange(users?.users ?? []);
        onViewProfile(null);
        return;
      }

      const selectedUsersList = users?.users.filter((user) => keys.has(user.id)) ?? [];
      onSelectedUsersChange(selectedUsersList);

      // Handle profile viewing
      if (selectedUsersList.length !== 1) {
        onViewProfile(null);
        return;
      }

      // Single user selected - check if we should auto-open profile
      if (isKeyboardNavigation) {
        return; // Don't auto-open on keyboard navigation
      }

      // For touch devices in single selection mode, always open profile
      if (isTouchDevice() || !isMediumViewportOrLarger()) {
        onViewProfile(selectedUsersList[0], false);
      } else if (isMediumViewportOrLarger()) {
        // For desktop, also open profile
        onViewProfile(selectedUsersList[0], false);
      }
    },
    [users?.users, onSelectedUsersChange, onViewProfile, isKeyboardNavigation]
  );

  // Handle Enter key to open profile
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key !== "Enter" || selectedUsers.length !== 1) {
        return;
      }

      const activeElement = document.activeElement;
      const tableContainer = document.querySelector(".min-h-0.flex-1");
      if (!tableContainer?.contains(activeElement)) {
        return;
      }

      const target = e.target as HTMLElement;
      if (target.tagName === "BUTTON" || target.closest("button")) {
        return;
      }

      e.preventDefault();
      e.stopPropagation();
      onViewProfile(selectedUsers[0], true);
    };

    document.addEventListener("keydown", handleKeyDown, true);
    return () => {
      document.removeEventListener("keydown", handleKeyDown, true);
    };
  }, [selectedUsers, onViewProfile]);

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

  return (
    <>
      <div className="min-h-48 flex-1">
        <Table
          key={pageOffset}
          selectionMode={isTouchDevice() || !isMediumViewportOrLarger() ? "single" : "multiple"}
          selectionBehavior="replace"
          selectedKeys={new Set(selectedUsers.map((user) => user.id))}
          onSelectionChange={handleSelectionChange}
          sortDescriptor={sortDescriptor}
          onSortChange={handleSortChange}
          aria-label={t`Users`}
          className={isMobile ? "[&>div>div>div]:-webkit-overflow-scrolling-touch" : ""}
          disableHorizontalScroll={isProfileOpen}
        >
          <TableHeader>
            <Column
              allowsSorting={true}
              id={SortableUserProperties.Name}
              isRowHeader={true}
              minWidth={isSmallViewportOrLarger() ? 250 : undefined}
            >
              <Trans>Name</Trans>
            </Column>
            {isSmallViewportOrLarger() && (
              <Column minWidth={160} allowsSorting={true} id={SortableUserProperties.Email}>
                <Trans>Email</Trans>
              </Column>
            )}
            {isMediumViewportOrLarger() && (
              <Column minWidth={65} defaultWidth={110} allowsSorting={true} id={SortableUserProperties.CreatedAt}>
                <Trans>Created</Trans>
              </Column>
            )}
            {isMediumViewportOrLarger() && (
              <Column minWidth={65} defaultWidth={140} allowsSorting={true} id={SortableUserProperties.ModifiedAt}>
                <Trans>Modified</Trans>
              </Column>
            )}
            {isSmallViewportOrLarger() && (
              <Column width={135} allowsSorting={true} id={SortableUserProperties.Role}>
                <Trans>Role</Trans>
              </Column>
            )}
          </TableHeader>
          <TableBody>
            {users?.users.map((user) => (
              <Row key={user.id} id={user.id}>
                <Cell>
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
                </Cell>
                {isSmallViewportOrLarger() && (
                  <Cell>
                    <Text className="h-full w-full justify-start p-0 text-left font-normal">{user.email}</Text>
                  </Cell>
                )}
                {isMediumViewportOrLarger() && (
                  <Cell>
                    <SmartDate date={user.createdAt} className="text-foreground" />
                  </Cell>
                )}
                {isMediumViewportOrLarger() && (
                  <Cell>
                    <SmartDate date={user.modifiedAt} className="text-foreground" />
                  </Cell>
                )}
                {isSmallViewportOrLarger() && (
                  <Cell>
                    <div className="flex h-full w-full items-center justify-between p-0">
                      <Badge variant="outline">{getUserRoleLabel(user.role)}</Badge>
                      <MenuTrigger
                        onOpenChange={(isOpen) => {
                          if (isOpen) {
                            onSelectedUsersChange([user]);
                          }
                        }}
                      >
                        <Button variant="icon" aria-label={t`User actions`}>
                          <EllipsisVerticalIcon className="h-5 w-5 text-muted-foreground" />
                        </Button>
                        <Menu>
                          <MenuItem id="viewProfile" onAction={() => onViewProfile(user, false)}>
                            <UserIcon className="h-4 w-4" />
                            <Trans>View profile</Trans>
                          </MenuItem>
                          {userInfo?.role === "Owner" && (
                            <>
                              <MenuItem
                                id="changeRole"
                                isDisabled={user.id === userInfo?.id}
                                onAction={() => onChangeRole(user)}
                              >
                                <SettingsIcon className="h-4 w-4" />
                                <Trans>Change role</Trans>
                              </MenuItem>
                              <MenuSeparator />
                              <MenuItem
                                id="deleteUser"
                                isDisabled={user.id === userInfo?.id}
                                onAction={() => onDeleteUser(user)}
                              >
                                <Trash2Icon className="h-4 w-4 text-destructive" />
                                <span className="text-destructive">
                                  <Trans>Delete</Trans>
                                </span>
                              </MenuItem>
                            </>
                          )}
                        </Menu>
                      </MenuTrigger>
                    </div>
                  </Cell>
                )}
              </Row>
            ))}
          </TableBody>
        </Table>
      </div>

      {/* Mobile: Loading indicator for infinite scroll */}
      {isMobile && <div ref={loadMoreRef} className="h-1" />}

      {/* Desktop: Regular pagination */}
      {!isMobile && users && (
        <div className="flex-shrink-0 bg-background pt-4">
          <Pagination
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
