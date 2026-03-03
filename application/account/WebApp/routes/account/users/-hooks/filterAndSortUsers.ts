import type { useUsers } from "@repo/infrastructure/sync/hooks";

import type { UserRole } from "@/shared/lib/api/client";
import type { UserStatus } from "@/shared/lib/api/userStatus";

import { SortableUserProperties, SortOrder } from "@/shared/lib/api/sortTypes";

type ElectricUser = ReturnType<typeof useUsers>["data"][number];

export function filterAndSortUsers(
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
