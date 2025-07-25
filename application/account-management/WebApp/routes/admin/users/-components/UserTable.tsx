import { SortOrder, SortableUserProperties, api, type components } from "@/shared/lib/api/client";
import { getUserRoleLabel } from "@/shared/lib/api/userRole";
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
import { MEDIA_QUERIES } from "@repo/ui/utils/responsive";
import { formatDate } from "@repo/utils/date/formatDate";
import { getInitials } from "@repo/utils/string/getInitials";
import { useNavigate, useSearch } from "@tanstack/react-router";
import { EllipsisVerticalIcon, SettingsIcon, Trash2Icon, UserIcon } from "lucide-react";
import { useCallback, useEffect, useState } from "react";
import type { Selection, SortDescriptor } from "react-aria-components";
import { MenuTrigger, TableBody } from "react-aria-components";

type UserDetails = components["schemas"]["UserDetails"];

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

  const [sortDescriptor, setSortDescriptor] = useState<SortDescriptor>(() => ({
    column: orderBy ?? "email",
    direction: sortOrder === "Ascending" ? "ascending" : "descending"
  }));
  const [isKeyboardNavigation, setIsKeyboardNavigation] = useState(false);

  const { data: users, isLoading } = api.useQuery("get", "/api/account-management/users", {
    params: {
      query: {
        Search: search,
        UserRole: userRole,
        UserStatus: userStatus,
        StartDate: startDate,
        EndDate: endDate,
        OrderBy: orderBy,
        SortOrder: sortOrder,
        PageOffset: pageOffset
      }
    }
  });

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
      navigate({
        to: "/admin/users",
        search: (prev) => ({
          ...prev,
          orderBy: (newSortDescriptor.column?.toString() ?? "Name") as SortableUserProperties,
          sortOrder: newSortDescriptor.direction === "ascending" ? SortOrder.Ascending : SortOrder.Descending,
          pageOffset: undefined
        })
      });
    },
    [navigate]
  );

  // biome-ignore lint/correctness/useExhaustiveDependencies: Clear selected users when page changes - pageOffset is needed to trigger the effect
  useEffect(() => {
    onSelectedUsersChange([]);
  }, [onSelectedUsersChange, pageOffset]);

  useEffect(() => {
    if (users?.users) {
      onUsersLoaded?.(users.users);
    }
  }, [users?.users, onUsersLoaded]);

  // Track keyboard vs mouse interaction
  useEffect(() => {
    const handleKeyDown = () => {
      setIsKeyboardNavigation(true);
    };

    const handleMouseDown = () => {
      setIsKeyboardNavigation(false);
    };

    const handlePointerDown = () => {
      setIsKeyboardNavigation(false);
    };

    // Use capture phase to ensure we set the flag before any click handlers
    document.addEventListener("keydown", handleKeyDown, true);
    document.addEventListener("mousedown", handleMouseDown, true);
    document.addEventListener("pointerdown", handlePointerDown, true);

    return () => {
      document.removeEventListener("keydown", handleKeyDown, true);
      document.removeEventListener("mousedown", handleMouseDown, true);
      document.removeEventListener("pointerdown", handlePointerDown, true);
    };
  }, []);

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
      const isSmallScreen = !window.matchMedia(MEDIA_QUERIES.md).matches;
      const shouldAutoOpen = !isSmallScreen || !isKeyboardNavigation;

      if (shouldAutoOpen) {
        onViewProfile(selectedUsersList[0], isKeyboardNavigation);
      }
    },
    [users?.users, onSelectedUsersChange, onViewProfile, isKeyboardNavigation]
  );

  // Handle Enter key to open profile
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      // Only handle if focus is within the table area
      const activeElement = document.activeElement;
      const tableContainer = document.querySelector(".min-h-0.flex-1");

      if (tableContainer?.contains(activeElement)) {
        if (e.key === "Enter" && selectedUsers.length === 1) {
          const target = e.target as HTMLElement;
          // Don't interfere with menu triggers or buttons
          if (target.tagName !== "BUTTON" && !target.closest("button")) {
            e.preventDefault();
            e.stopPropagation();
            onViewProfile(selectedUsers[0], true);
          }
        }
      }
    };

    // Add the handler for all screen sizes
    document.addEventListener("keydown", handleKeyDown, true);
    return () => {
      document.removeEventListener("keydown", handleKeyDown, true);
    };
  }, [selectedUsers, onViewProfile]);

  if (isLoading) {
    return null;
  }

  const currentPage = (users?.currentPageOffset ?? 0) + 1;
  const isSmallScreen = typeof window !== "undefined" && !window.matchMedia(MEDIA_QUERIES.md).matches;

  return (
    <div className="flex h-full flex-col">
      <div className="min-h-0 flex-1">
        <Table
          key={`${search}-${userRole}-${userStatus}-${startDate}-${endDate}-${orderBy}-${sortOrder}`}
          selectionMode={isSmallScreen ? "single" : "multiple"}
          selectionBehavior="replace"
          selectedKeys={new Set(selectedUsers.map((user) => user.id))}
          onSelectionChange={handleSelectionChange}
          sortDescriptor={sortDescriptor}
          onSortChange={handleSortChange}
          aria-label={t`Users`}
        >
          <TableHeader>
            <Column minWidth={180} allowsSorting={true} id={SortableUserProperties.Name} isRowHeader={true}>
              <Trans>Name</Trans>
            </Column>
            <Column minWidth={120} allowsSorting={true} id={SortableUserProperties.Email}>
              <Trans>Email</Trans>
            </Column>
            <Column minWidth={65} defaultWidth={110} allowsSorting={true} id={SortableUserProperties.CreatedAt}>
              <Trans>Created</Trans>
            </Column>
            <Column minWidth={65} defaultWidth={120} allowsSorting={true} id={SortableUserProperties.ModifiedAt}>
              <Trans>Modified</Trans>
            </Column>
            <Column minWidth={100} defaultWidth={75} allowsSorting={true} id={SortableUserProperties.Role}>
              <Trans>Role</Trans>
            </Column>
            <Column width={100}>
              <Trans>Actions</Trans>
            </Column>
          </TableHeader>
          <TableBody>
            {users?.users.map((user) => (
              <Row key={user.id} id={user.id}>
                <Cell>
                  <Text className="flex h-14 w-full items-center justify-start gap-2 p-0 text-left font-normal">
                    <Avatar
                      initials={getInitials(user.firstName, user.lastName, user.email)}
                      avatarUrl={user.avatarUrl}
                      size="sm"
                      isRound={true}
                    />
                    <Text className="flex flex-col truncate">
                      <Text className="truncate text-foreground">
                        {user.firstName} {user.lastName}
                        {user.emailConfirmed ? (
                          ""
                        ) : (
                          <Badge variant="outline">
                            <Trans>Pending</Trans>
                          </Badge>
                        )}
                      </Text>
                      <Text className="truncate">{user.title ?? ""}</Text>
                    </Text>
                  </Text>
                </Cell>
                <Cell>
                  <Text className="h-full w-full justify-start p-0 text-left font-normal">{user.email}</Text>
                </Cell>
                <Cell>
                  <Text className="h-full w-full justify-start p-0 text-left font-normal">
                    {formatDate(user.createdAt)}
                  </Text>
                </Cell>
                <Cell>
                  <Text className="h-full w-full justify-start p-0 text-left font-normal">
                    {formatDate(user.modifiedAt)}
                  </Text>
                </Cell>
                <Cell>
                  <Text className="h-full w-full justify-start p-0 text-left font-normal">
                    <Badge variant="outline">{getUserRoleLabel(user.role)}</Badge>
                  </Text>
                </Cell>
                <Cell>
                  <Text className="flex w-full justify-end">
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
                  </Text>
                </Cell>
              </Row>
            ))}
          </TableBody>
        </Table>
      </div>

      {users && (
        <div className="bg-background py-4 max-sm:sticky max-sm:bottom-0 max-sm:border-border max-sm:border-t">
          <Pagination
            paginationSize={5}
            currentPage={currentPage}
            totalPages={users.totalPages ?? 1}
            onPageChange={handlePageChange}
            className="w-full pr-12 sm:hidden"
          />
          <Pagination
            paginationSize={9}
            currentPage={currentPage}
            totalPages={users.totalPages ?? 1}
            onPageChange={handlePageChange}
            previousLabel={t`Previous`}
            nextLabel={t`Next`}
            className="hidden w-full sm:flex"
          />
        </div>
      )}
    </div>
  );
}
