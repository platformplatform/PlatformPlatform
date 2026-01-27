import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { DateRangePicker, parseDateString } from "@repo/ui/components/DateRangePicker";
import {
  Dialog,
  DialogBody,
  DialogClose,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle
} from "@repo/ui/components/Dialog";
import { Field, FieldLabel } from "@repo/ui/components/Field";
import { SearchField } from "@repo/ui/components/SearchField";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@repo/ui/components/Select";
import { Tooltip, TooltipContent, TooltipTrigger } from "@repo/ui/components/Tooltip";
import { useDebounce } from "@repo/ui/hooks/useDebounce";
import { useSideMenuLayout } from "@repo/ui/hooks/useSideMenuLayout";
import { useLocation, useNavigate } from "@tanstack/react-router";
import { format } from "date-fns";
import { Filter, FilterX } from "lucide-react";
import { useCallback, useEffect, useRef, useState } from "react";
import { type SortableUserProperties, type SortOrder, UserRole, UserStatus } from "@/shared/lib/api/client";
import { getUserRoleLabel } from "@/shared/lib/api/userRole";
import { getUserStatusLabel } from "@/shared/lib/api/userStatus";

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

interface UserQueryingProps {
  onFiltersUpdated?: () => void;
  onFiltersExpandedChange?: (expanded: boolean) => void;
}

// Thresholds based on max content widths (Danish language, long dates)
// Max expanded filters: 838px, Button icon-only: 44px, Gap: 8px
const THRESHOLD_FILTERS_EXPANDED = 890; // 838 + 44 + 8

function hasSpaceForInlineFilters(toolbarWidth: number): boolean {
  // Show inline filters when there's room for filters + icon-only button
  // Button text will be decided separately based on remaining space
  return toolbarWidth >= THRESHOLD_FILTERS_EXPANDED;
}

export function UserQuerying({ onFiltersUpdated, onFiltersExpandedChange }: UserQueryingProps = {}) {
  const navigate = useNavigate();
  const searchParams = (useLocation().search as SearchParams) ?? {};
  const { isOverlayOpen, isMobileMenuOpen } = useSideMenuLayout();
  const containerRef = useRef<HTMLDivElement>(null);
  const [search, setSearch] = useState(searchParams.search ?? "");
  const debouncedSearch = useDebounce(search, 500);
  const [showAllFilters, setShowAllFilters] = useState(
    Boolean(searchParams.userRole ?? searchParams.userStatus ?? searchParams.startDate ?? searchParams.endDate)
  );
  const [isFilterPanelOpen, setIsFilterPanelOpen] = useState(false);

  const dateRange =
    searchParams.startDate && searchParams.endDate
      ? {
          start: parseDateString(searchParams.startDate),
          end: parseDateString(searchParams.endDate)
        }
      : null;

  const updateFilter = useCallback(
    (params: Partial<SearchParams>, isSearchUpdate = false) => {
      navigate({
        to: "/admin/users",
        search: (prev) => ({
          ...prev,
          ...params,
          pageOffset: undefined
        })
      });
      if (!isSearchUpdate) {
        onFiltersUpdated?.();
      }
    },
    [navigate, onFiltersUpdated]
  );

  useEffect(() => {
    navigate({
      to: "/admin/users",
      search: (prev) => ({
        ...prev,
        search: debouncedSearch || undefined,
        pageOffset: undefined
      })
    });
  }, [debouncedSearch, navigate]);

  const activeFilterCount =
    (searchParams.userRole ? 1 : 0) +
    (searchParams.userStatus ? 1 : 0) +
    (searchParams.startDate && searchParams.endDate ? 1 : 0);

  useEffect(() => {
    const toolbar = containerRef.current?.closest(".flex.items-center.justify-between") as HTMLElement;
    if (!toolbar) {
      return;
    }

    let lastToggle = 0;
    const check = () => {
      const now = Date.now();
      if (now - lastToggle < 200) {
        return;
      }

      if (isOverlayOpen || isMobileMenuOpen) {
        if (showAllFilters) {
          lastToggle = now;
          setShowAllFilters(false);
        }
        return;
      }

      const hasSpace = hasSpaceForInlineFilters(toolbar.offsetWidth);
      if (hasSpace && activeFilterCount > 0 && !showAllFilters) {
        lastToggle = now;
        setShowAllFilters(true);
      } else if (!hasSpace && showAllFilters) {
        lastToggle = now;
        setShowAllFilters(false);
      }
    };

    const observer = new ResizeObserver(check);
    observer.observe(toolbar);
    check();
    return () => observer.disconnect();
  }, [activeFilterCount, showAllFilters, isOverlayOpen, isMobileMenuOpen]);

  // Notify parent of filter expansion state changes
  useEffect(() => {
    onFiltersExpandedChange?.(showAllFilters);
  }, [showAllFilters, onFiltersExpandedChange]);

  const clearAllFilters = () => {
    setSearch("");

    updateFilter({
      search: undefined,
      userRole: undefined,
      userStatus: undefined,
      startDate: undefined,
      endDate: undefined
    });

    setShowAllFilters(false);
    setIsFilterPanelOpen(false);
  };

  return (
    <div ref={containerRef} className="flex items-center gap-2">
      <SearchField
        placeholder={t`Search`}
        value={search}
        onChange={setSearch}
        label={t`Search`}
        className={showAllFilters ? "w-60 shrink-0" : "min-w-32 max-w-60 flex-1"}
      />

      {showAllFilters && (
        <>
          <Field className="flex flex-col">
            <FieldLabel>{t`User role`}</FieldLabel>
            <Select
              value={searchParams.userRole ?? ""}
              onValueChange={(userRole) => {
                updateFilter({ userRole: (userRole as UserRole) || undefined });
              }}
            >
              <SelectTrigger aria-label={t`User role`} className="min-w-28">
                <SelectValue>
                  {(value: string) => (value ? getUserRoleLabel(value as UserRole) : t`Any role`)}
                </SelectValue>
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="">
                  <Trans>Any role</Trans>
                </SelectItem>
                {Object.values(UserRole).map((userRole) => (
                  <SelectItem value={userRole} key={userRole}>
                    {getUserRoleLabel(userRole)}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </Field>

          <Field className="flex flex-col">
            <FieldLabel>{t`User status`}</FieldLabel>
            <Select
              value={searchParams.userStatus ?? ""}
              onValueChange={(userStatus) => {
                updateFilter({ userStatus: (userStatus as UserStatus) || undefined });
              }}
            >
              <SelectTrigger aria-label={t`User status`} className="min-w-28">
                <SelectValue>
                  {(value: string) => (value ? getUserStatusLabel(value as UserStatus) : t`Any status`)}
                </SelectValue>
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="">
                  <Trans>Any status</Trans>
                </SelectItem>
                {Object.values(UserStatus).map((userStatus) => (
                  <SelectItem value={userStatus} key={userStatus}>
                    {getUserStatusLabel(userStatus)}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </Field>

          <DateRangePicker
            value={dateRange}
            onChange={(range) => {
              updateFilter({
                startDate: range ? format(range.start, "yyyy-MM-dd") : undefined,
                endDate: range ? format(range.end, "yyyy-MM-dd") : undefined
              });
            }}
            label={t`Modified date`}
            placeholder={t`Select dates`}
          />
        </>
      )}

      <Tooltip>
        <TooltipTrigger
          render={
            <Button
              variant="secondary"
              size="icon"
              className={showAllFilters ? "relative mt-auto" : "relative mt-8"}
              aria-label={showAllFilters ? t`Clear filters` : t`Show filters`}
              data-testid="filter-button"
              onClick={() => {
                if (showAllFilters) {
                  clearAllFilters();
                  return;
                }

                const toolbar = containerRef.current?.closest(".flex.items-center.justify-between") as HTMLElement;
                if (!isOverlayOpen && !isMobileMenuOpen && toolbar && hasSpaceForInlineFilters(toolbar.offsetWidth)) {
                  setShowAllFilters(true);
                } else {
                  setIsFilterPanelOpen(true);
                }
              }}
            >
              {showAllFilters ? (
                <FilterX size={16} aria-label={t`Clear filters`} />
              ) : (
                <Filter size={16} aria-label={t`Show filters`} />
              )}
              {activeFilterCount > 0 && !showAllFilters && (
                <span className="absolute -top-1 -right-1 flex size-5 items-center justify-center rounded-full bg-primary font-medium text-primary-foreground text-xs">
                  {activeFilterCount}
                </span>
              )}
            </Button>
          }
        />
        <TooltipContent>{showAllFilters ? <Trans>Clear filters</Trans> : <Trans>Show filters</Trans>}</TooltipContent>
      </Tooltip>

      <Dialog open={isFilterPanelOpen} onOpenChange={setIsFilterPanelOpen}>
        <DialogContent className="w-full sm:min-w-[400px]">
          <DialogHeader>
            <DialogTitle>
              <Trans>Filters</Trans>
            </DialogTitle>
          </DialogHeader>

          <DialogBody>
            <SearchField
              placeholder={t`Search`}
              value={search}
              onChange={setSearch}
              label={t`Search`}
              className="w-full"
            />

            <Field className="flex w-full flex-col">
              <FieldLabel>{t`User role`}</FieldLabel>
              <Select
                value={searchParams.userRole ?? ""}
                onValueChange={(userRole) => {
                  updateFilter({ userRole: (userRole as UserRole) || undefined });
                }}
              >
                <SelectTrigger className="w-full" aria-label={t`User role`}>
                  <SelectValue>
                    {(value: string) => (value ? getUserRoleLabel(value as UserRole) : t`Any role`)}
                  </SelectValue>
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="">
                    <Trans>Any role</Trans>
                  </SelectItem>
                  {Object.values(UserRole).map((userRole) => (
                    <SelectItem value={userRole} key={userRole}>
                      {getUserRoleLabel(userRole)}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </Field>

            <Field className="flex w-full flex-col">
              <FieldLabel>{t`User status`}</FieldLabel>
              <Select
                value={searchParams.userStatus ?? ""}
                onValueChange={(userStatus) => {
                  updateFilter({ userStatus: (userStatus as UserStatus) || undefined });
                }}
              >
                <SelectTrigger className="w-full" aria-label={t`User status`}>
                  <SelectValue>
                    {(value: string) => (value ? getUserStatusLabel(value as UserStatus) : t`Any status`)}
                  </SelectValue>
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="">
                    <Trans>Any status</Trans>
                  </SelectItem>
                  {Object.values(UserStatus).map((userStatus) => (
                    <SelectItem value={userStatus} key={userStatus}>
                      {getUserStatusLabel(userStatus)}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </Field>

            <DateRangePicker
              value={dateRange}
              onChange={(range) => {
                updateFilter({
                  startDate: range ? format(range.start, "yyyy-MM-dd") : undefined,
                  endDate: range ? format(range.end, "yyyy-MM-dd") : undefined
                });
              }}
              label={t`Modified date`}
              placeholder={t`Select dates`}
              className="w-full"
            />
          </DialogBody>

          <DialogFooter>
            <Button
              variant="secondary"
              onClick={clearAllFilters}
              disabled={activeFilterCount === 0 && !searchParams.search}
            >
              <Trans>Clear</Trans>
            </Button>
            <DialogClose render={<Button variant="default" />}>
              <Trans>OK</Trans>
            </DialogClose>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
