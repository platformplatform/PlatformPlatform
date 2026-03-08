import { useViewportResize } from "@repo/ui/hooks/useViewportResize";
import { keepPreviousData } from "@tanstack/react-query";
import { useSearch } from "@tanstack/react-router";

import { api, type components } from "@/shared/lib/api/client";

import { useInfiniteUsers } from "../-hooks/useInfiniteUsers";
import { UserTableContent } from "./UserTableContent";

type UserDetails = components["schemas"]["UserDetails"];

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
    "/api/account/users",
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

  const hasFilters = Boolean(search || userRole || userStatus || startDate || endDate);

  return (
    <UserTableContent
      {...props}
      usersList={data?.users ?? []}
      isLoading={isLoading}
      isMobile={false}
      totalPages={data?.totalPages ?? 1}
      currentPageOffset={data?.currentPageOffset ?? 0}
      hasFilters={hasFilters}
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

  const hasFilters = Boolean(search || userRole || userStatus || startDate || endDate);

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
      hasFilters={hasFilters}
    />
  );
}
