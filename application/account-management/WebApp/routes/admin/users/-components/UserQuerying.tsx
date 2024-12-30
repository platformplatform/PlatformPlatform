import { FilterIcon, FilterXIcon } from "lucide-react";
import { useCallback, useEffect, useState } from "react";
import { Button } from "@repo/ui/components/Button";
import { DateRangePicker } from "@repo/ui/components/DateRangePicker";
import { SearchField } from "@repo/ui/components/SearchField";
import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { useLocation, useNavigate } from "@tanstack/react-router";
import { type SortableUserProperties, type SortOrder, UserRole, UserStatus } from "@/shared/lib/api/client";
import { Select, SelectItem } from "@repo/ui/components/Select";
import { getUserRoleLabel } from "@/shared/lib/api/userRole";
import { getUserStatusLabel } from "@/shared/lib/api/userStatus";
import { parseDate, type DateValue } from "@internationalized/date";

// SearchParams interface defines the structure of URL query parameters
interface SearchParams {
  search: string | undefined;
  userRole: UserRole | undefined;
  userStatus: UserStatus | undefined;
  startDate: string | undefined;
  endDate: string | undefined;
  orderBy: SortableUserProperties | undefined;
  sortOrder: SortOrder | undefined;
  pageOffset: number | undefined;
}

type DateRange = { start: DateValue; end: DateValue } | null;

/**
 * UserQuerying component handles the user list filtering.
 * Uses URL parameters as the single source of truth for all filters.
 * The only local state is for the search input, which is debounced
 * to prevent too many URL updates while typing.
 */
export function UserQuerying() {
  const navigate = useNavigate();
  const searchParams = (useLocation().search as SearchParams) ?? {};
  const [search, setSearch] = useState<string | undefined>(searchParams.search);
  const [showAllFilters, setShowAllFilters] = useState(
    Boolean(searchParams.userRole ?? searchParams.userStatus ?? searchParams.startDate ?? searchParams.endDate)
  );

  // Convert URL date strings to DateRange if they exist
  const dateRange =
    searchParams.startDate && searchParams.endDate
      ? {
          start: parseDate(searchParams.startDate),
          end: parseDate(searchParams.endDate)
        }
      : null;

  // Updates URL parameters while preserving existing ones
  const updateFilter = useCallback(
    (params: Partial<SearchParams>) => {
      navigate({
        to: "/admin/users",
        search: (prev) => ({
          ...prev,
          ...params,
          pageOffset: prev.pageOffset === 0 ? undefined : prev.pageOffset
        })
      });
    },
    [navigate]
  );

  // Debounce search updates to avoid too many URL changes while typing
  useEffect(() => {
    const timeoutId = setTimeout(() => {
      updateFilter({ search: (search as string) || undefined });
    }, 500);

    return () => clearTimeout(timeoutId);
  }, [search, updateFilter]);

  return (
    <div className="flex items-center mt-4 mb-4 gap-2">
      <SearchField
        placeholder={t`Search`}
        value={search}
        onChange={setSearch}
        label={t`Search`}
        autoFocus
        className="min-w-[240px]"
      />

      {showAllFilters && (
        <>
          <Select
            selectedKey={searchParams.userRole}
            onSelectionChange={(userRole) => {
              updateFilter({ userRole: (userRole as UserRole) || undefined });
            }}
            label={t`User Role`}
            placeholder={t`Any role`}
            className="w-[150px]"
          >
            <SelectItem id="">
              <Trans>Any role</Trans>
            </SelectItem>
            {Object.values(UserRole).map((userRole) => (
              <SelectItem id={userRole} key={userRole}>
                {getUserRoleLabel(userRole)}
              </SelectItem>
            ))}
          </Select>

          <Select
            selectedKey={searchParams.userStatus}
            onSelectionChange={(userStatus) => {
              updateFilter({ userStatus: (userStatus as UserStatus) || undefined });
            }}
            label={t`User Status`}
            placeholder={t`Any status`}
            className="w-[150px]"
          >
            <SelectItem id="">
              <Trans>Any status</Trans>
            </SelectItem>
            {Object.values(UserStatus).map((userStatus) => (
              <SelectItem id={userStatus} key={userStatus}>
                {getUserStatusLabel(userStatus)}
              </SelectItem>
            ))}
          </Select>

          <DateRangePicker
            value={dateRange}
            onChange={(range) => {
              updateFilter({
                startDate: range?.start.toString() || undefined,
                endDate: range?.end.toString() || undefined
              });
            }}
            label={t`Creation date`}
          />
        </>
      )}

      <Button
        variant="secondary"
        className={showAllFilters ? "h-10 w-10 p-0 mt-6" : "mt-6"}
        onPress={() => {
          if (showAllFilters) {
            // Reset filters when hiding
            updateFilter({ userRole: undefined, userStatus: undefined, startDate: undefined, endDate: undefined });
          }
          setShowAllFilters(!showAllFilters);
        }}
      >
        {showAllFilters ? (
          <FilterXIcon size={16} aria-label={t`Hide filters`} />
        ) : (
          <FilterIcon size={16} aria-label={t`Show filters`} />
        )}
      </Button>
    </div>
  );
}
