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
  onViewProfile: (user: UserDetails | null) => void;
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

  useEffect(() => {
    onSelectedUsersChange([]);
  }, [onSelectedUsersChange]);

  useEffect(() => {
    if (users?.users) {
      onUsersLoaded?.(users.users);
    }
  }, [users?.users, onUsersLoaded]);

  const handleSelectionChange = useCallback(
    (keys: Selection) => {
      if (keys === "all") {
        onSelectedUsersChange(users?.users ?? []);
        // Close profile when selecting all users
        onViewProfile(null);
      } else {
        const selectedKeys = typeof keys === "string" ? new Set([keys]) : keys;
        const selectedUsersList = users?.users.filter((user) => selectedKeys.has(user.id)) ?? [];
        onSelectedUsersChange(selectedUsersList);

        // Handle profile viewing based on selection
        if (selectedUsersList.length === 1) {
          // Single user selected - show profile
          onViewProfile(selectedUsersList[0]);
        } else {
          // Multiple users selected or no users selected - close profile
          onViewProfile(null);
        }
      }
    },
    [users?.users, onSelectedUsersChange, onViewProfile]
  );

  if (isLoading) {
    return null;
  }

  const currentPage = (users?.currentPageOffset ?? 0) + 1;

  return (
    <div className="flex h-full flex-col">
      <div className="min-h-0 flex-1">
        <Table
          key={`${search}-${userRole}-${userStatus}-${startDate}-${endDate}-${orderBy}-${sortOrder}`}
          selectionMode="multiple"
          selectionBehavior="replace"
          selectedKeys={selectedUsers.map((user) => user.id)}
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
            <Column width={80}>
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
                        <MenuItem id="viewProfile" onAction={() => onViewProfile(user)}>
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
        <div className="bg-background py-4">
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
