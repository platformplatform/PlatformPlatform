import { useCallback, useEffect, useState } from "react";
import { SearchField } from "@repo/ui/components/SearchField";
import { t } from "@lingui/core/macro";
import { useNavigate, useLocation } from "@tanstack/react-router";
import { UserRole, type SortableUserProperties, type SortOrder } from "@/shared/lib/api/client";
import { DateRangePicker } from "@repo/ui/components/DateRangePicker";
import { Select, SelectItem } from "@repo/ui/components/Select";
import type { Key } from "@repo/ui/components/Select";
import { CalendarDate, type DateValue } from "@internationalized/date";
import { getUserRoleLabel } from "@/shared/lib/api/userRole";
import { type UserStatus, USER_STATUSES, getStatusLabel } from "@/shared/lib/api/userStatus";

interface RoleOption {
  id: UserRole;
  label: string;
}

interface StatusOption {
  id: UserStatus;
  label: string;
}

type DateRange = {
  start: DateValue;
  end: DateValue;
} | null;

interface SearchParams {
  search?: string;
  pageOffset?: number;
  orderBy?: SortableUserProperties;
  sortOrder?: SortOrder;
  userRole?: string;
  status?: string;
  startDate?: string;
  endDate?: string;
}

export function UserQuerying() {
  const navigate = useNavigate();
  const location = useLocation();

  const searchParams = location.search as SearchParams;
  const {
    search: urlSearch = "",
    userRole,
    status: urlStatus,
    startDate: urlStartDate,
    endDate: urlEndDate
  } = searchParams ?? {};

  const USER_ROLE_OPTIONS: RoleOption[] = Object.values(UserRole).map((role) => ({
    id: role,
    label: getUserRoleLabel(role)
  }));

  const [search, setSearch] = useState<string>(urlSearch);
  const [selectedRole, setSelectedRole] = useState<Key | null>(userRole ?? null);
  const [selectedStatus, setSelectedStatus] = useState<Key | null>(urlStatus ?? null);
  const [dateRange, setDateRange] = useState<DateRange>(() => {
    if (!urlStartDate || !urlEndDate) return null;

    try {
      return {
        start: new CalendarDate(
          Number.parseInt(urlStartDate.slice(0, 4)),
          Number.parseInt(urlStartDate.slice(5, 7)),
          Number.parseInt(urlStartDate.slice(8, 10))
        ),
        end: new CalendarDate(
          Number.parseInt(urlEndDate.slice(0, 4)),
          Number.parseInt(urlEndDate.slice(5, 7)),
          Number.parseInt(urlEndDate.slice(8, 10))
        )
      };
    } catch {
      return null;
    }
  });

  const STATUS_OPTIONS: StatusOption[] = USER_STATUSES.map((status) => ({
    id: status,
    label: getStatusLabel(status)
  }));

  const updateSearch = useCallback(
    (value: string) => {
      navigate({
        to: "/admin/users",
        search: (prev) => ({
          ...prev,
          search: value || undefined,
          pageOffset: prev.pageOffset === 0 ? undefined : prev.pageOffset,
          userRole: selectedRole ? (selectedRole as UserRole) : undefined,
          status: selectedStatus ?? undefined,
          startDate: dateRange?.start?.toString() ?? undefined,
          endDate: dateRange?.end?.toString() ?? undefined
        })
      });
    },
    [navigate, selectedRole, selectedStatus, dateRange]
  );

  useEffect(() => {
    const timeoutId = setTimeout(() => {
      updateSearch(search);
    }, 700);

    return () => clearTimeout(timeoutId);
  }, [search, updateSearch]);

  useEffect(() => {
    setSelectedRole(userRole ?? null);
  }, [userRole]);

  useEffect(() => {
    setSelectedStatus(urlStatus ?? null);
  }, [urlStatus]);

  useEffect(() => {
    setSearch(urlSearch);
  }, [urlSearch]);

  const handleSearchChange = (value: string) => {
    setSearch(value);
  };

  return (
    <div className="flex items-center mt-4 mb-4 gap-4">
      <div className="flex flex-col gap-2">
        <SearchField
          placeholder={t`Search`}
          value={search}
          onChange={handleSearchChange}
          label={t`Search`}
          autoFocus
          className="min-w-[200px]"
        />
      </div>

      <Select
        selectedKey={selectedRole}
        onSelectionChange={setSelectedRole}
        items={USER_ROLE_OPTIONS}
        label={t`Role`}
        placeholder={t`Select role`}
        className="w-[150px]"
      >
        {(item) => <SelectItem id={item.id}>{item.label}</SelectItem>}
      </Select>

      <Select
        selectedKey={selectedStatus}
        onSelectionChange={setSelectedStatus}
        items={STATUS_OPTIONS}
        label={t`Status`}
        placeholder={t`Select status`}
        className="w-[150px]"
      >
        {(item) => <SelectItem id={item.id}>{item.label}</SelectItem>}
      </Select>

      <DateRangePicker value={dateRange} onChange={setDateRange} label={t`Creation date`} />
    </div>
  );
}
