import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { trackInteraction } from "@repo/infrastructure/applicationInsights/ApplicationInsightsProvider";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { useUsers } from "@repo/infrastructure/sync/hooks";
import { Avatar, AvatarFallback, AvatarImage } from "@repo/ui/components/Avatar";
import { Badge } from "@repo/ui/components/Badge";
import { Button } from "@repo/ui/components/Button";
import {
  ContextMenu,
  ContextMenuContent,
  ContextMenuItem,
  ContextMenuSeparator,
  ContextMenuTrigger
} from "@repo/ui/components/ContextMenu";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger
} from "@repo/ui/components/DropdownMenu";
import { Empty, EmptyDescription, EmptyHeader, EmptyMedia, EmptyTitle } from "@repo/ui/components/Empty";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@repo/ui/components/Table";
import { useViewportResize } from "@repo/ui/hooks/useViewportResize";
import { getInitials } from "@repo/utils/string/getInitials";
import { useNavigate, useSearch } from "@tanstack/react-router";
import { ArrowUp, EllipsisVerticalIcon, SearchIcon, SettingsIcon, Trash2Icon, UserIcon } from "lucide-react";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { SmartDate } from "@/shared/components/SmartDate";
import type { UserRole } from "@/shared/lib/api/client";
import { SortableUserProperties, SortOrder } from "@/shared/lib/api/sortTypes";
import { getUserRoleLabel } from "@/shared/lib/api/userRole";
import type { UserStatus } from "@/shared/lib/api/userStatus";

type ElectricUser = ReturnType<typeof useUsers>["data"][number];

type SortDescriptor = {
  column: string;
  direction: "ascending" | "descending";
};

interface UserTableProps {
  selectedUsers: ElectricUser[];
  onSelectedUsersChange: (users: ElectricUser[]) => void;
  onViewProfile: (user: ElectricUser | null, isKeyboardOpen?: boolean) => void;
  onDeleteUser: (user: ElectricUser) => void;
  onChangeRole: (user: ElectricUser) => void;
  onUsersLoaded?: (users: ElectricUser[]) => void;
}

function filterAndSortUsers(
  users: ElectricUser[],
  params: {
    search?: string;
    userRole?: UserRole | null;
    userStatus?: UserStatus | null;
    startDate?: string;
    endDate?: string;
    orderBy?: string;
    sortOrder?: string;
  }
): ElectricUser[] {
  let filtered = users;

  if (params.search) {
    const searchLower = params.search.toLowerCase();
    filtered = filtered.filter(
      (user) =>
        user.firstName?.toLowerCase().includes(searchLower) ||
        user.lastName?.toLowerCase().includes(searchLower) ||
        user.email.toLowerCase().includes(searchLower) ||
        user.title?.toLowerCase().includes(searchLower)
    );
  }

  if (params.userRole) {
    filtered = filtered.filter((user) => user.role === params.userRole);
  }

  if (params.userStatus === "Active") {
    filtered = filtered.filter((user) => user.emailConfirmed);
  } else if (params.userStatus === "Pending") {
    filtered = filtered.filter((user) => !user.emailConfirmed);
  }

  if (params.startDate) {
    const startDate = new Date(params.startDate);
    filtered = filtered.filter((user) => user.lastSeenAt && new Date(user.lastSeenAt) >= startDate);
  }

  if (params.endDate) {
    const endDate = new Date(params.endDate);
    endDate.setHours(23, 59, 59, 999);
    filtered = filtered.filter((user) => user.lastSeenAt && new Date(user.lastSeenAt) <= endDate);
  }

  const orderBy = params.orderBy ?? SortableUserProperties.Name;
  const isDescending = params.sortOrder === SortOrder.Descending;

  filtered.sort((a, b) => {
    let comparison = 0;

    switch (orderBy) {
      case SortableUserProperties.Name: {
        const nameA = `${a.firstName ?? ""} ${a.lastName ?? ""}`.trim().toLowerCase();
        const nameB = `${b.firstName ?? ""} ${b.lastName ?? ""}`.trim().toLowerCase();
        comparison = nameA.localeCompare(nameB);
        break;
      }
      case SortableUserProperties.Email:
        comparison = a.email.localeCompare(b.email);
        break;
      case SortableUserProperties.CreatedAt:
        comparison = a.createdAt.localeCompare(b.createdAt);
        break;
      case SortableUserProperties.LastSeenAt:
        comparison = (a.lastSeenAt ?? "").localeCompare(b.lastSeenAt ?? "");
        break;
      case SortableUserProperties.Role:
        comparison = a.role.localeCompare(b.role);
        break;
    }

    return isDescending ? -comparison : comparison;
  });

  return filtered;
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
    [navigate, sortDescriptor]
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
        <TableHeader className="z-10 bg-inherit sm:sticky sm:top-0">
          <TableRow>
            <TableHead
              data-column={SortableUserProperties.Name}
              className="cursor-pointer select-none"
              onClick={() => handleSortChange(SortableUserProperties.Name)}
            >
              <div className="flex items-center gap-1 font-bold text-xs">
                <span>
                  <Trans>Name</Trans>
                </span>
                <SortIndicator sortDescriptor={sortDescriptor} columnId={SortableUserProperties.Name} />
              </div>
            </TableHead>
            {!isMobile && (
              <>
                <TableHead
                  data-column={SortableUserProperties.Email}
                  className="cursor-pointer select-none"
                  onClick={() => handleSortChange(SortableUserProperties.Email)}
                >
                  <div className="flex items-center gap-1 font-bold text-xs">
                    <span>
                      <Trans>Email</Trans>
                    </span>
                    <SortIndicator sortDescriptor={sortDescriptor} columnId={SortableUserProperties.Email} />
                  </div>
                </TableHead>
                <TableHead
                  data-column={SortableUserProperties.CreatedAt}
                  className="w-[7rem] min-w-[4rem] cursor-pointer select-none"
                  onClick={() => handleSortChange(SortableUserProperties.CreatedAt)}
                >
                  <div className="flex items-center gap-1 font-bold text-xs">
                    <span>
                      <Trans>Created</Trans>
                    </span>
                    <SortIndicator sortDescriptor={sortDescriptor} columnId={SortableUserProperties.CreatedAt} />
                  </div>
                </TableHead>
                <TableHead
                  data-column={SortableUserProperties.LastSeenAt}
                  className="w-[7.5rem] min-w-[4rem] cursor-pointer select-none"
                  onClick={() => handleSortChange(SortableUserProperties.LastSeenAt)}
                >
                  <div className="flex items-center gap-1 font-bold text-xs">
                    <span>
                      <Trans>Last seen</Trans>
                    </span>
                    <SortIndicator sortDescriptor={sortDescriptor} columnId={SortableUserProperties.LastSeenAt} />
                  </div>
                </TableHead>
                <TableHead
                  data-column={SortableUserProperties.Role}
                  className="w-[8.5rem] cursor-pointer select-none"
                  onClick={() => handleSortChange(SortableUserProperties.Role)}
                >
                  <div className="flex items-center gap-1 font-bold text-xs">
                    <span>
                      <Trans>Role</Trans>
                    </span>
                    <SortIndicator sortDescriptor={sortDescriptor} columnId={SortableUserProperties.Role} />
                  </div>
                </TableHead>
              </>
            )}
          </TableRow>
        </TableHeader>
        <TableBody>
          {usersList.map((user, index) => {
            const isSelected = selectedUserIds.has(user.id);
            const userRowContent = (
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
                          : isMobile
                            ? user.email
                            : ""}
                      </span>
                      {!isMobile && !user.emailConfirmed && (
                        <Badge variant="outline" className="shrink-0">
                          <Trans>Pending</Trans>
                        </Badge>
                      )}
                    </div>
                    {isMobile && !user.emailConfirmed ? (
                      <Badge variant="outline" className="mt-1 -ml-2 w-fit">
                        <Trans>Pending</Trans>
                      </Badge>
                    ) : (
                      <span className="block truncate text-muted-foreground text-sm">{user.title ?? ""}</span>
                    )}
                  </div>
                </div>
              </div>
            );

            return (
              <TableRow
                key={user.id}
                data-state={isSelected ? "selected" : undefined}
                className={`cursor-pointer select-none ${isSelected ? "bg-active-background hover:bg-active-background" : "hover:bg-hover-background"}`}
                onClick={(event) => handleRowClick(user, event)}
                index={index}
              >
                <TableCell className="pr-8">
                  {isMobile ? (
                    <ContextMenu
                      onOpenChange={(isOpen) => {
                        if (isOpen) {
                          onSelectedUsersChange([user]);
                          trackInteraction("User actions", "menu", "Open");
                        }
                      }}
                    >
                      <ContextMenuTrigger className="block w-full">{userRowContent}</ContextMenuTrigger>
                      <ContextMenuContent className="w-auto">
                        <ContextMenuItem onClick={() => onViewProfile(user, false)}>
                          <UserIcon className="size-4" />
                          <Trans>View profile</Trans>
                        </ContextMenuItem>
                        {userInfo?.role === "Owner" && (
                          <>
                            <ContextMenuItem disabled={user.id === userInfo?.id} onClick={() => onChangeRole(user)}>
                              <SettingsIcon className="size-4" />
                              <Trans>Change role</Trans>
                            </ContextMenuItem>
                            <ContextMenuSeparator />
                            <ContextMenuItem
                              disabled={user.id === userInfo?.id}
                              variant="destructive"
                              onClick={() => onDeleteUser(user)}
                            >
                              <Trash2Icon className="size-4" />
                              <Trans>Delete</Trans>
                            </ContextMenuItem>
                          </>
                        )}
                      </ContextMenuContent>
                    </ContextMenu>
                  ) : (
                    userRowContent
                  )}
                </TableCell>
                {!isMobile && (
                  <>
                    <TableCell>
                      <span className="block h-full w-full justify-start truncate p-0 text-left font-normal">
                        {user.email}
                      </span>
                    </TableCell>
                    <TableCell>
                      <SmartDate date={user.createdAt} className="text-foreground" />
                    </TableCell>
                    <TableCell>
                      <SmartDate date={user.lastSeenAt} className="text-foreground" />
                    </TableCell>
                    <TableCell>
                      <div className="flex h-full w-full items-center justify-between p-0">
                        <Badge variant="outline">{getUserRoleLabel(user.role as UserRole)}</Badge>
                        <DropdownMenu
                          onOpenChange={(isOpen) => {
                            if (isOpen) {
                              onSelectedUsersChange([user]);
                              trackInteraction("User actions", "menu", "Open");
                            }
                          }}
                        >
                          <DropdownMenuTrigger
                            render={
                              <Button variant="ghost" size="icon" tabIndex={-1} aria-label={t`User actions`}>
                                <EllipsisVerticalIcon className="size-5 text-muted-foreground" />
                              </Button>
                            }
                          />
                          <DropdownMenuContent className="w-auto">
                            <DropdownMenuItem onClick={() => onViewProfile(user, false)}>
                              <UserIcon className="size-4" />
                              <Trans>View profile</Trans>
                            </DropdownMenuItem>
                            {userInfo?.role === "Owner" && (
                              <>
                                <DropdownMenuItem
                                  disabled={user.id === userInfo?.id}
                                  onClick={() => onChangeRole(user)}
                                >
                                  <SettingsIcon className="size-4" />
                                  <Trans>Change role</Trans>
                                </DropdownMenuItem>
                                <DropdownMenuSeparator />
                                <DropdownMenuItem
                                  disabled={user.id === userInfo?.id}
                                  variant="destructive"
                                  onClick={() => onDeleteUser(user)}
                                >
                                  <Trash2Icon className="size-4" />
                                  <Trans>Delete</Trans>
                                </DropdownMenuItem>
                              </>
                            )}
                          </DropdownMenuContent>
                        </DropdownMenu>
                      </div>
                    </TableCell>
                  </>
                )}
              </TableRow>
            );
          })}
        </TableBody>
      </Table>
    </div>
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
      className={`flex size-4 items-center justify-center transition ${sortDescriptor.direction === "descending" ? "rotate-180" : ""}`}
    >
      <ArrowUp aria-hidden={true} className="size-4 text-muted-foreground" />
    </span>
  );
}
