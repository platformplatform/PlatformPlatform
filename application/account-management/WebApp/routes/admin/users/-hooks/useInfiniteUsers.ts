import { useCallback, useEffect, useState } from "react";
import {
  api,
  type components,
  SortableUserProperties,
  SortOrder,
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

  // Initial load query
  const { data: initialData, isLoading: isInitialLoading } = api.useQuery("get", "/api/account-management/users", {
    params: {
      query: {
        Search: search,
        UserRole: userRole,
        UserStatus: userStatus,
        StartDate: startDate,
        EndDate: endDate,
        OrderBy: orderBy ?? SortableUserProperties.Name,
        SortOrder: sortOrder ?? SortOrder.Ascending,
        PageOffset: 0
      }
    },
    enabled
  });

  // State for next page to load
  const [nextPageToLoad, setNextPageToLoad] = useState<number | null>(null);

  // Load more query
  const { data: moreData } = api.useQuery("get", "/api/account-management/users", {
    params: {
      query: {
        Search: search,
        UserRole: userRole,
        UserStatus: userStatus,
        StartDate: startDate,
        EndDate: endDate,
        OrderBy: orderBy ?? SortableUserProperties.Name,
        SortOrder: sortOrder ?? SortOrder.Ascending,
        PageOffset: nextPageToLoad
      }
    },
    enabled: enabled && nextPageToLoad !== null && isLoadingMore
  });

  // Reset when filters change
  useEffect(() => {
    if (initialData) {
      setAllUsers(initialData.users || []);
      setTotalPages(initialData.totalPages || 1);
      setCurrentPage(0);
      setNextPageToLoad(null);
    }
  }, [initialData]);

  // Append more data when loaded
  useEffect(() => {
    if (moreData && isLoadingMore) {
      setAllUsers((prev) => [...prev, ...(moreData.users || [])]);
      setIsLoadingMore(false);
      setNextPageToLoad(null);
    }
  }, [moreData, isLoadingMore]);

  const loadMore = useCallback(() => {
    if (isLoadingMore || !totalPages || currentPage >= totalPages - 1) {
      return;
    }

    const nextPage = currentPage + 1;
    setCurrentPage(nextPage);
    setNextPageToLoad(nextPage);
    setIsLoadingMore(true);
  }, [currentPage, totalPages, isLoadingMore]);

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
