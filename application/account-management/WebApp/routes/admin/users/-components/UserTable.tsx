import { SortOrder, SortableUserProperties, UserRole, api, type components } from "@/shared/lib/api/client";
import { getUserRoleLabel } from "@/shared/lib/api/userRole";
import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { AlertDialog } from "@repo/ui/components/AlertDialog";
import { Avatar } from "@repo/ui/components/Avatar";
import { Badge } from "@repo/ui/components/Badge";
import { Button } from "@repo/ui/components/Button";
import { Menu, MenuItem, MenuSeparator } from "@repo/ui/components/Menu";
import { Modal } from "@repo/ui/components/Modal";
import { Pagination } from "@repo/ui/components/Pagination";
import { Select, SelectItem } from "@repo/ui/components/Select";
import { Cell, Column, Row, Table, TableHeader } from "@repo/ui/components/Table";
import { useNavigate, useSearch } from "@tanstack/react-router";
import { EllipsisVerticalIcon, PencilIcon, Trash2Icon, UserIcon } from "lucide-react";
import { useCallback, useState } from "react";
import type { Selection, SortDescriptor } from "react-aria-components";
import { MenuTrigger, TableBody } from "react-aria-components";
import { DeleteUserDialog } from "./DeleteUserDialog";

type UserDetails = components["schemas"]["UserDetails"];

interface UserTableProps {
  selectedUsers: UserDetails[];
  onSelectedUsersChange: (users: UserDetails[]) => void;
  onRefreshNeeded: () => void;
}

export function UserTable({ selectedUsers, onSelectedUsersChange, onRefreshNeeded }: Readonly<UserTableProps>) {
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

  const handleDelete = useCallback(() => {
    if (!userToDelete) {
      return;
    }
    onSelectedUsersChange(selectedUsers.filter((user) => user.id !== userToDelete.id));
    onRefreshNeeded();
    setUserToDelete(null);
  }, [userToDelete, onRefreshNeeded, onSelectedUsersChange, selectedUsers]);

  const changeUserRoleMutation = api.useMutation("put", "/api/account-management/users/{id}/change-user-role");

  const handleUserRoleChange = useCallback(
    async (newUserRole: UserRole) => {
      if (!userToChangeRole) {
        return;
      }

      await changeUserRoleMutation.mutateAsync({
        params: { path: { id: userToChangeRole.id } },
        body: { userRole: newUserRole }
      });

      onRefreshNeeded();
      setUserToChangeRole(null);
    },
    [userToChangeRole, changeUserRoleMutation, onRefreshNeeded]
  );

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
      <Modal
        isOpen={userToChangeRole !== null}
        onOpenChange={() => setUserToChangeRole(null)}
        blur={false}
        isDismissable={true}
      >
        <AlertDialog title={t`Change user role`}>
          <p className="text-muted-foreground text-sm">
            <Trans>
              Select a new role for{" "}
              <b>
                {`${userToChangeRole?.firstName ?? ""} ${userToChangeRole?.lastName ?? ""}`.trim() ||
                  userToChangeRole?.email}
              </b>
            </Trans>
          </p>

          <div className="mt-4 flex flex-col gap-4">
            <Select
              autoFocus={true}
              aria-label={t`User role`}
              selectedKey={userToChangeRole?.role}
              onSelectionChange={(key) => handleUserRoleChange(key as UserRole)}
              className="flex w-full flex-col"
            >
              {Object.values(UserRole).map((userRole) => (
                <SelectItem id={userRole} key={userRole}>
                  {getUserRoleLabel(userRole)}
                </SelectItem>
              ))}
            </Select>
          </div>
        </AlertDialog>
      </Modal>

      <DeleteUserDialog
        users={userToDelete ? [userToDelete] : []}
        isOpen={userToDelete !== null}
        onOpenChange={(isOpen) => !isOpen && setUserToDelete(null)}
        onSuccess={handleDelete}
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
                <Cell>{toFormattedDate(user.createdAt)}</Cell>
                <Cell>{toFormattedDate(user.modifiedAt)}</Cell>
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

function toFormattedDate(input: string | undefined | null) {
  if (!input) {
    return "";
  }
  const date = new Date(input);
  return date.toLocaleDateString(undefined, { day: "numeric", month: "short", year: "numeric" });
}

function getInitials(firstName: string | undefined, lastName: string | undefined, email: string | undefined) {
  if (firstName && lastName) {
    return `${firstName[0]}${lastName[0]}`;
  }
  if (email == null) {
    return "";
  }
  return email.split("@")[0].slice(0, 2).toUpperCase();
}
