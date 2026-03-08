import type { UserRole } from "@/shared/lib/api/client";
import type { SortableUserProperties, SortOrder } from "@/shared/lib/api/sortTypes";
import type { UserStatus } from "@/shared/lib/api/userStatus";

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
