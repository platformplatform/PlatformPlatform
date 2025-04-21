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
import { formatDate } from "@repo/utils/date/formatDate";
import { getInitials } from "@repo/utils/string/getInitials";
import { useNavigate, useSearch } from "@tanstack/react-router";
import { EllipsisVerticalIcon, PencilIcon, Trash2Icon, UserIcon } from "lucide-react";
import { useCallback, useEffect, useState } from "react";
import type { Selection, SortDescriptor } from "react-aria-components";
import { MenuTrigger, TableBody } from "react-aria-components";
import { ChangeUserRoleDialog } from "./ChangeUserRoleDialog";
import { DeleteUserDialog } from "./DeleteUserDialog";

type UserDetails = components["schemas"]["UserDetails"];

interface UserTableProps {
  selectedUsers: UserDetails[];
  onSelectedUsersChange: (users: UserDetails[]) => void;
}

export function UserTable({ selectedUsers, onSelectedUsersChange }: Readonly<UserTableProps>) {
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

  const [userToDelete, setUserToDelete] = useState<UserDetails | null>(null);
  const [userToChangeRole, setUserToChangeRole] = useState<UserDetails | null>(null);

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

  const handleSelectionChange = useCallback(
    (keys: Selection) => {
      if (keys === "all") {
        onSelectedUsersChange(users?.users ?? []);
      } else {
        const selectedKeys = typeof keys === "string" ? new Set([keys]) : keys;
        const selectedUsersList = users?.users.filter((user) => selectedKeys.has(user.id)) ?? [];
        onSelectedUsersChange(selectedUsersList);
      }
    },
    [users?.users, onSelectedUsersChange]
  );

  if (isLoading) {
    return null;
  }

  const currentPage = (users?.currentPageOffset ?? 0) + 1;

  return (
    <>
      <ChangeUserRoleDialog
        user={userToChangeRole}
        isOpen={userToChangeRole !== null}
        onOpenChange={(isOpen) => !isOpen && setUserToChangeRole(null)}
      />

      <DeleteUserDialog
        users={userToDelete ? [userToDelete] : []}
        isOpen={userToDelete !== null}
        onOpenChange={(isOpen) => !isOpen && setUserToDelete(null)}
      />

      <div className="flex h-full w-full flex-col gap-2">
        <Table
          key={`${search}-${userRole}-${userStatus}-${startDate}-${endDate}-${orderBy}-${sortOrder}`}
          selectionMode="multiple"
          selectionBehavior="toggle"
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
            <Column width={114}>
              <Trans>Actions</Trans>
            </Column>
          </TableHeader>
          <TableBody>
            {users?.users.map((user) => (
              <Row key={user.id} id={user.id}>
                <Cell>
                  <div className="flex h-14 items-center gap-2">
                    <Avatar
                      initials={getInitials(user.firstName, user.lastName, user.email)}
                      avatarUrl={user.avatarUrl}
                      size="sm"
                      isRound={true}
                    />
                    <div className="flex flex-col truncate">
                      <div className="truncate text-foreground">
                        {user.firstName} {user.lastName}
                        {user.emailConfirmed ? (
                          ""
                        ) : (
                          <Badge variant="outline">
                            <Trans>Pending</Trans>
                          </Badge>
                        )}
                      </div>
                      <div className="truncate">{user.title ?? ""}</div>
                    </div>
                  </div>
                </Cell>
                <Cell>{user.email}</Cell>
                <Cell>{formatDate(user.createdAt)}</Cell>
                <Cell>{formatDate(user.modifiedAt)}</Cell>
                <Cell>
                  <Badge variant="outline">{getUserRoleLabel(user.role)}</Badge>
                </Cell>
                <Cell>
                  <div className="group flex w-full gap-2">
                    <Button
                      variant="icon"
                      className="opacity-0 transition-opacity duration-300 ease-in-out group-hover:opacity-100"
                      onPress={() => {
                        onSelectedUsersChange([user]);
                        setUserToDelete(user);
                      }}
                      isDisabled={user.id === userInfo?.id}
                    >
                      <Trash2Icon className="h-5 w-5 text-muted-foreground" />
                    </Button>
                    <MenuTrigger
                      onOpenChange={(isOpen) => {
                        if (isOpen) {
                          onSelectedUsersChange([user]);
                        }
                      }}
                    >
                      <Button variant="icon" aria-label={t`Menu`}>
                        <EllipsisVerticalIcon className="h-5 w-5 text-muted-foreground" />
                      </Button>
                      <Menu>
                        <MenuItem id="viewProfile">
                          <UserIcon className="h-4 w-4" />
                          <Trans>View profile</Trans>
                        </MenuItem>
                        <MenuItem
                          id="changeRole"
                          isDisabled={userInfo?.role !== "Owner" || userInfo?.id === user.id}
                          onAction={() => setUserToChangeRole(user)}
                        >
                          <PencilIcon className="h-4 w-4 group-disabled:text-muted-foreground" />
                          <span className="group-disabled:text-muted-foreground">
                            <Trans>Change role</Trans>
                          </span>
                        </MenuItem>
                        <MenuSeparator />
                        <MenuItem
                          id="deleteUser"
                          isDisabled={userInfo?.role !== "Owner" || user.id === userInfo?.id}
                          onAction={() => setUserToDelete(user)}
                        >
                          <Trash2Icon className="h-4 w-4 text-destructive" />
                          <span className="text-destructive">
                            <Trans>Delete</Trans>
                          </span>
                        </MenuItem>
                      </Menu>
                    </MenuTrigger>
                  </div>
                </Cell>
              </Row>
            ))}
          </TableBody>
        </Table>
        {users && (
          <>
            <Pagination
              paginationSize={5}
              currentPage={currentPage}
              totalPages={users?.totalPages ?? 1}
              onPageChange={handlePageChange}
              className="w-full pr-12 sm:hidden"
            />
            <Pagination
              paginationSize={9}
              currentPage={currentPage}
              totalPages={users?.totalPages ?? 1}
              onPageChange={handlePageChange}
              previousLabel={t`Previous`}
              nextLabel={t`Next`}
              className="hidden w-full sm:flex"
            />
          </>
        )}
      </div>
    </>
  );
}
