import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { Avatar, AvatarFallback, AvatarImage } from "@repo/ui/components/Avatar";
import { Badge } from "@repo/ui/components/Badge";
import { Button } from "@repo/ui/components/Button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger
} from "@repo/ui/components/DropdownMenu";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@repo/ui/components/Table";
import { TablePagination } from "@repo/ui/components/TablePagination";
import { useInfiniteScroll } from "@repo/ui/hooks/useInfiniteScroll";
import { useViewportResize } from "@repo/ui/hooks/useViewportResize";
import { isMediumViewportOrLarger, isSmallViewportOrLarger } from "@repo/ui/utils/responsive";
import { getInitials } from "@repo/utils/string/getInitials";
import { keepPreviousData } from "@tanstack/react-query";
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

export function UserTable(props: Readonly<UserTableProps>) {
  const isMobile = useViewportResize();
  return isMobile ? <MobileUserTable {...props} /> : <DesktopUserTable {...props} />;
}

function DesktopUserTable(props: Readonly<UserTableProps>) {
  const { search, userRole, userStatus, startDate, endDate, orderBy, sortOrder, pageOffset } = useSearch({
    strict: false
  });

  const { data, isLoading } = api.useQuery(
    "get",
    "/api/account-management/users",
    {
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
    },
    { placeholderData: keepPreviousData }
  );

  return (
    <UserTableContent
      {...props}
      usersList={data?.users ?? []}
      isLoading={isLoading}
      isMobile={false}
      totalPages={data?.totalPages ?? 1}
      currentPageOffset={data?.currentPageOffset ?? 0}
    />
  );
}

function MobileUserTable(props: Readonly<UserTableProps>) {
  const { search, userRole, userStatus, startDate, endDate, orderBy, sortOrder } = useSearch({
    strict: false
  });

  const { users, isLoading, isLoadingMore, hasMore, loadMore } = useInfiniteUsers({
    search,
    userRole,
    userStatus,
    startDate,
    endDate,
    orderBy,
    sortOrder,
    enabled: true
  });

  return (
    <UserTableContent
      {...props}
      usersList={users}
      isLoading={isLoading}
      isMobile={true}
      totalPages={1}
      currentPageOffset={0}
      isLoadingMore={isLoadingMore}
      hasMore={hasMore}
      loadMore={loadMore}
    />
  );
}

interface UserTableContentProps extends UserTableProps {
  usersList: UserDetails[];
  isLoading: boolean;
  isMobile: boolean;
  totalPages: number;
  currentPageOffset: number;
  isLoadingMore?: boolean;
  hasMore?: boolean;
  loadMore?: () => void;
}

function UserTableContent({
  selectedUsers,
  onSelectedUsersChange,
  onViewProfile,
  onDeleteUser,
  onChangeRole,
  onUsersLoaded,
  usersList,
  isLoading,
  isMobile,
  totalPages,
  currentPageOffset,
  isLoadingMore = false,
  hasMore = false,
  loadMore
}: Readonly<UserTableContentProps>) {
  const navigate = useNavigate();
  const { orderBy, sortOrder, pageOffset } = useSearch({ strict: false });
  const userInfo = useUserInfo();

  const [sortDescriptor, setSortDescriptor] = useState<SortDescriptor>(() => ({
    column: orderBy ?? SortableUserProperties.Name,
    direction: sortOrder === SortOrder.Descending ? "descending" : "ascending"
  }));
  const selectedUserIds = useMemo(() => new Set(selectedUsers.map((user) => user.id)), [selectedUsers]);

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
      onSelectedUsersChange([]);

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

  const previousPageOffset = useRef(pageOffset);
  useEffect(() => {
    if (previousPageOffset.current !== pageOffset) {
      previousPageOffset.current = pageOffset;
      onSelectedUsersChange([]);
    }
  }, [onSelectedUsersChange, pageOffset]);

  const previousUserIds = useRef<string>("");
  useEffect(() => {
    const userIds = usersList.map((u) => u.id).join(",");
    if (userIds !== previousUserIds.current) {
      previousUserIds.current = userIds;
      onUsersLoaded?.(usersList);
    }
  }, [usersList, onUsersLoaded]);

  const handleRowClick = useCallback(
    (user: UserDetails, event: React.MouseEvent) => {
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

  const loadMoreRef = useInfiniteScroll({
    enabled: isMobile,
    hasMore,
    isLoadingMore,
    onLoadMore: loadMore ?? (() => {})
  });

  if (isLoading && usersList.length === 0) {
    return null;
  }

  const currentPage = currentPageOffset + 1;

  return (
    <>
      <div className="min-h-48 flex-1 overflow-auto rounded-md outline-ring focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2">
        <Table
          aria-label={t`Users`}
          selectedIndex={currentSelectedIndex}
          onNavigate={(index) => onSelectedUsersChange([usersList[index]])}
          onActivate={(index) => onViewProfile(usersList[index], true)}
        >
          <TableHeader className="sticky top-0 z-10 bg-inherit">
            <TableRow>
              <TableHead
                data-column={SortableUserProperties.Name}
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
                  data-column={SortableUserProperties.Email}
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
                  data-column={SortableUserProperties.CreatedAt}
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
                  data-column={SortableUserProperties.LastSeenAt}
                  className="w-[120px] min-w-[65px] cursor-pointer select-none"
                  onClick={() => handleSortChange(SortableUserProperties.LastSeenAt)}
                >
                  <div className="flex items-center gap-1 font-bold text-xs">
                    <span>
                      <Trans>Last seen</Trans>
                    </span>
                    <SortIndicator sortDescriptor={sortDescriptor} columnId={SortableUserProperties.LastSeenAt} />
                  </div>
                </TableHead>
              )}
              {isSmallViewportOrLarger() && (
                <TableHead
                  data-column={SortableUserProperties.Role}
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
            {usersList.map((user, index) => {
              const isSelected = selectedUserIds.has(user.id);
              return (
                <TableRow
                  key={user.id}
                  data-state={isSelected ? "selected" : undefined}
                  className={`cursor-pointer select-none ${isSelected ? "bg-active-background hover:bg-active-background" : "hover:bg-hover-background"}`}
                  onClick={(event) => handleRowClick(user, event)}
                  index={index}
                >
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
                            {user.emailConfirmed ? null : (
                              <Badge variant="outline" className="shrink-0">
                                <Trans>Pending</Trans>
                              </Badge>
                            )}
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
                      <SmartDate date={user.createdAt} className="text-foreground" />
                    </TableCell>
                  )}
                  {isMediumViewportOrLarger() && (
                    <TableCell>
                      <SmartDate date={user.lastSeenAt} className="text-foreground" />
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
                  )}
                </TableRow>
              );
            })}
          </TableBody>
        </Table>
        {isMobile && <div ref={loadMoreRef} className="h-1" />}
      </div>

      {!isMobile && (
        <div className="flex-shrink-0 pt-4">
          <TablePagination
            currentPage={currentPage}
            totalPages={totalPages}
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
      className={`flex size-4 items-center justify-center transition ${sortDescriptor.direction === "descending" ? "rotate-180" : ""}`}
    >
      <ArrowUp aria-hidden={true} className="size-4 text-muted-foreground" />
    </span>
  );
}
