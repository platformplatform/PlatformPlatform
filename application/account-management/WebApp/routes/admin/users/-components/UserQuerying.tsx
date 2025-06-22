import { type SortOrder, type SortableUserProperties, UserRole, UserStatus } from "@/shared/lib/api/client";
import { getUserRoleLabel } from "@/shared/lib/api/userRole";
import { getUserStatusLabel } from "@/shared/lib/api/userStatus";
import { parseDate } from "@internationalized/date";
import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { DateRangePicker } from "@repo/ui/components/DateRangePicker";
import { Dialog } from "@repo/ui/components/Dialog";
import { Heading } from "@repo/ui/components/Heading";
import { Modal } from "@repo/ui/components/Modal";
import { SearchField } from "@repo/ui/components/SearchField";
import { Select, SelectItem } from "@repo/ui/components/Select";
import { MEDIA_QUERIES } from "@repo/ui/utils/responsive";
import { useLocation, useNavigate } from "@tanstack/react-router";
import { ListFilter, ListFilterPlus, XIcon } from "lucide-react";
import { useCallback, useEffect, useState } from "react";

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
  const [searchTimeoutId, setSearchTimeoutId] = useState<NodeJS.Timeout | null>(null);
  const [isFilterPanelOpen, setIsFilterPanelOpen] = useState(false);

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
      setSearchTimeoutId(null);
    }, 500);
    setSearchTimeoutId(timeoutId);

    return () => {
      clearTimeout(timeoutId);
      setSearchTimeoutId(null);
    };
  }, [search, updateFilter]);

  // Count active filters for badge
  const getActiveFilterCount = () => {
    let count = 0;
    if (searchParams.userRole) {
      count++;
    }
    if (searchParams.userStatus) {
      count++;
    }
    if (searchParams.startDate && searchParams.endDate) {
      count++;
    }
    return count;
  };

  const activeFilterCount = getActiveFilterCount();

  // Handle screen size changes to show/hide filters appropriately
  useEffect(() => {
    const handleResize = () => {
      const isLargeScreen = window.matchMedia(MEDIA_QUERIES.lg).matches;
      if (isLargeScreen && activeFilterCount > 0 && !showAllFilters) {
        // On large screens, show inline filters if there are active filters
        setShowAllFilters(true);
      } else if (!isLargeScreen && showAllFilters) {
        // On small/medium screens, hide inline filters
        setShowAllFilters(false);
      }
    };

    // Check on mount
    handleResize();

    window.addEventListener("resize", handleResize);
    return () => window.removeEventListener("resize", handleResize);
  }, [activeFilterCount, showAllFilters]);

  const clearAllFilters = () => {
    updateFilter({ userRole: undefined, userStatus: undefined, startDate: undefined, endDate: undefined });
    setShowAllFilters(false);
    setIsFilterPanelOpen(false);
  };

  return (
    <div className="flex items-center gap-2">
      <SearchField
        placeholder={t`Search`}
        value={search}
        onChange={setSearch}
        onSubmit={() => {
          if (searchTimeoutId) {
            clearTimeout(searchTimeoutId);
            setSearchTimeoutId(null);
          }
          updateFilter({ search: (search as string) || undefined });
        }}
        label={t`Search`}
        autoFocus={true}
      />

      {showAllFilters && (
        <>
          <Select
            selectedKey={searchParams.userRole}
            onSelectionChange={(userRole) => {
              updateFilter({ userRole: (userRole as UserRole) || undefined });
            }}
            label={t`User role`}
            placeholder={t`Any role`}
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
            label={t`User status`}
            placeholder={t`Any status`}
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
                startDate: range?.start.toString() ?? undefined,
                endDate: range?.end.toString() ?? undefined
              });
            }}
            label={t`Modified date`}
            placeholder={t`Select dates`}
          />
        </>
      )}

      {/* Filter button with responsive behavior */}
      <Button
        variant="secondary"
        className="relative mt-6"
        aria-label={showAllFilters ? t`Clear filters` : t`Show filters`}
        data-testid="filter-button"
        onPress={() => {
          // On large screens, if filters are showing, clear them instead of opening dialog
          const isLargeScreen = window.matchMedia(MEDIA_QUERIES.lg).matches;
          if (isLargeScreen && showAllFilters) {
            clearAllFilters();
            return;
          }
          // On large screens, toggle inline filters
          if (isLargeScreen) {
            setShowAllFilters(!showAllFilters);
            return;
          }
          // On small/medium screens, open dialog
          setIsFilterPanelOpen(true);
        }}
      >
        {showAllFilters ? (
          <ListFilterPlus size={16} aria-label={t`Clear filters`} />
        ) : (
          <ListFilter size={16} aria-label={t`Show filters`} />
        )}
        {activeFilterCount > 0 && (
          <span className="-right-1 -top-1 absolute flex h-5 w-5 items-center justify-center rounded-full bg-primary font-medium text-primary-foreground text-xs lg:hidden">
            {activeFilterCount}
          </span>
        )}
      </Button>

      {/* Filter dialog for small/medium screens */}
      <Modal isOpen={isFilterPanelOpen} onOpenChange={setIsFilterPanelOpen} isDismissable={true}>
        <Dialog className="w-full sm:min-w-[400px]">
          <XIcon
            onClick={() => setIsFilterPanelOpen(false)}
            className="absolute top-2 right-2 h-10 w-10 p-2 hover:bg-muted"
          />
          <Heading slot="title" className="text-2xl">
            <Trans>Filters</Trans>
          </Heading>

          <div className="mt-4 flex flex-col gap-4">
            <Select
              selectedKey={searchParams.userRole}
              onSelectionChange={(userRole) => {
                updateFilter({ userRole: (userRole as UserRole) || undefined });
              }}
              label={t`User role`}
              placeholder={t`Any role`}
              className="w-full"
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
              label={t`User status`}
              placeholder={t`Any status`}
              className="w-full"
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
                  startDate: range?.start.toString() ?? undefined,
                  endDate: range?.end.toString() ?? undefined
                });
              }}
              label={t`Modified date`}
              placeholder={t`Select dates`}
              className="w-full"
            />
          </div>

          <div className="mt-6 flex justify-end gap-4">
            <Button variant="secondary" onPress={clearAllFilters} isDisabled={activeFilterCount === 0}>
              <Trans>Clear</Trans>
            </Button>
            <Button variant="primary" onPress={() => setIsFilterPanelOpen(false)}>
              <Trans>OK</Trans>
            </Button>
          </div>
        </Dialog>
      </Modal>
    </div>
  );
}
