import { useCallback, useEffect, useState } from "react";
import {
  api,
  apiClient,
  type components,
  type SortableUserProperties,
  type SortOrder,
  type UserRole,
  type UserStatus
} from "@/shared/lib/api/client";

type UserDetails = components["schemas"]["UserDetails"];

interface UseInfiniteUsersParams {
  search?: string;
  userRole?: UserRole | null;
  userStatus?: UserStatus | null;
  startDate?: string;
  endDate?: string;
  orderBy?: SortableUserProperties;
  sortOrder?: SortOrder;
  enabled: boolean;
}

export function useInfiniteUsers({
  search,
  userRole,
  userStatus,
  startDate,
  endDate,
  orderBy,
  sortOrder,
  enabled
}: UseInfiniteUsersParams) {
  const [allUsers, setAllUsers] = useState<UserDetails[]>([]);
  const [currentPage, setCurrentPage] = useState(0);
  const [totalPages, setTotalPages] = useState<number | null>(null);
  const [isLoadingMore, setIsLoadingMore] = useState(false);

  const { data: initialData, isLoading: isInitialLoading } = api.useQuery(
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
          SortOrder: sortOrder
        }
      }
    },
    { enabled }
  );

  useEffect(() => {
    if (enabled && initialData) {
      setAllUsers(initialData.users || []);
      setTotalPages(initialData.totalPages || 1);
      setCurrentPage(0);
    }
  }, [enabled, initialData]);

  const loadMore = useCallback(async () => {
    if (isLoadingMore || !totalPages || currentPage >= totalPages - 1) {
      return;
    }

    const nextPage = currentPage + 1;
    setIsLoadingMore(true);

    try {
      const { data } = await apiClient.GET("/api/account-management/users", {
        params: {
          query: {
            Search: search,
            UserRole: userRole,
            UserStatus: userStatus,
            StartDate: startDate,
            EndDate: endDate,
            OrderBy: orderBy,
            SortOrder: sortOrder,
            PageOffset: nextPage
          }
        }
      });

      if (data) {
        setAllUsers((prev) => [...prev, ...(data.users || [])]);
        setCurrentPage(nextPage);
      }
    } finally {
      setIsLoadingMore(false);
    }
  }, [currentPage, totalPages, isLoadingMore, search, userRole, userStatus, startDate, endDate, orderBy, sortOrder]);

  const hasMore = totalPages !== null && currentPage < totalPages - 1;

  return {
    users: allUsers,
    isLoading: isInitialLoading,
    isLoadingMore,
    hasMore,
    loadMore,
    totalPages
  };
}
