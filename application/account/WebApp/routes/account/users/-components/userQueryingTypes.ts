import type { SortableUserProperties, SortOrder, UserRole, UserStatus } from "@/shared/lib/api/client";

export interface SearchParams {
  search: string | undefined;
  userRole: UserRole | undefined;
  userStatus: UserStatus | undefined;
  startDate: string | undefined;
  endDate: string | undefined;
  orderBy: SortableUserProperties | undefined;
  sortOrder: SortOrder | undefined;
  pageOffset: number | undefined;
}

export type FilterUpdateFn = (params: Partial<SearchParams>, isSearchUpdate?: boolean) => void;
