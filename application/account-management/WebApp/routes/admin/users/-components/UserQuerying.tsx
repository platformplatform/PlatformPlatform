import { useCallback, useEffect, useState } from "react";
import { SearchField } from "@repo/ui/components/SearchField";
import { t } from "@lingui/core/macro";
import { useNavigate, useLocation } from "@tanstack/react-router";
import { UserRole, type SortableUserProperties, type SortOrder } from "@/shared/lib/api/client";
import { DateRangePicker } from "@repo/ui/components/DateRangePicker";
import { Select, SelectItem } from "@repo/ui/components/Select";
import type { Key } from "@repo/ui/components/Select";
import { CalendarDate, type DateValue } from "@internationalized/date";

type UserStatus = "Active" | "Pending";

const USER_STATUSES: UserStatus[] = ["Active", "Pending"];

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
  role?: string;
  status?: string;
  startDate?: string;
  endDate?: string;
}

const STATUS_OPTIONS: StatusOption[] = USER_STATUSES.map((status) => ({
  id: status,
  label: status
}));

export function UserQuerying() {
  const navigate = useNavigate();
  const location = useLocation();

  const searchParams = location.search as SearchParams;
  const {
    search: urlSearch = "",
    role: urlRole,
    status: urlStatus,
    startDate: urlStartDate,
    endDate: urlEndDate
  } = searchParams ?? {};

  const ROLE_OPTIONS: RoleOption[] = Object.values(UserRole).map((role) => ({
    id: role,
    label: role
  }));

  const [search, setSearch] = useState<string>(urlSearch);
  const [selectedRole, setSelectedRole] = useState<Key | null>(urlRole === "null" ? null : urlRole ?? null);
  const [selectedStatus, setSelectedStatus] = useState<Key | null>(urlStatus === "null" ? null : urlStatus ?? null);
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

  const updateSearch = useCallback(
    (value: string) => {
      navigate({
        to: "/admin/users",
        search: (prev) => ({
          ...prev,
          search: value || undefined,
          pageOffset: prev.pageOffset === 0 ? undefined : prev.pageOffset,
          role: selectedRole ?? undefined,
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

  const handleSearchChange = (value: string) => {
    setSearch(value);
  };

  useEffect(() => {
    setSearch(urlSearch);
  }, [urlSearch]);

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
        items={ROLE_OPTIONS}
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
