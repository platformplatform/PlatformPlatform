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
  onFilterStateChange?: (
    isFilterBarExpanded: boolean,
    hasActiveFilters: boolean,
    shouldUseCompactButtons: boolean
  ) => void;
  onFiltersUpdated?: () => void;
}

export function UserQuerying({ onFilterStateChange, onFiltersUpdated }: UserQueryingProps = {}) {
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
  const [, forceUpdate] = useState({});

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

  const [isSidePaneOpen, setIsSidePaneOpen] = useState(false);

  useEffect(() => {
    const checkSidePaneState = () => {
      const sidePane = document.querySelector('[class*="fixed"][class*="inset-0"][class*="z-70"]');
      const isOpen = !!sidePane;
      if (isOpen !== isSidePaneOpen) {
        setIsSidePaneOpen(isOpen);
      }
    };

    checkSidePaneState();

    const observer = new MutationObserver(checkSidePaneState);
    observer.observe(document.body, { childList: true, subtree: true });

    return () => observer.disconnect();
  }, [isSidePaneOpen]);

  useEffect(() => {
    let debounceTimeout: NodeJS.Timeout | null = null;
    let lastStateChange = 0;

    const shouldSkipSpaceCheck = (now: number) => {
      return now - lastStateChange < 200;
    };

    const shouldHideFiltersForOverlays = () => {
      return isOverlayOpen || isMobileMenuOpen;
    };

    const getToolbarContainer = () => {
      if (!containerRef.current) {
        return null;
      }
      return containerRef.current.closest(".flex.items-center.justify-between") as HTMLElement;
    };

    const calculateAvailableSpace = (toolbarContainer: HTMLElement, sidePaneOpen: boolean) => {
      const toolbarWidth = toolbarContainer.offsetWidth;
      const searchField = containerRef.current?.querySelector('input[type="text"]') as HTMLElement;
      const filterButton = containerRef.current?.querySelector('[data-testid="filter-button"]') as HTMLElement;

      const searchWidth = searchField?.offsetWidth || 300;
      const filterButtonWidth = filterButton?.offsetWidth || 50;
      const rightSideWidth = sidePaneOpen ? 200 : 150;
      const gaps = 24;

      const usedSpace = searchWidth + filterButtonWidth + rightSideWidth + gaps;
      return toolbarWidth - usedSpace;
    };

    const updateFiltersVisibility = (hasSpace: boolean, now: number) => {
      if (hasSpace && activeFilterCount > 0 && !showAllFilters) {
        lastStateChange = now;
        setShowAllFilters(true);
      } else if (!hasSpace && showAllFilters) {
        lastStateChange = now;
        setShowAllFilters(false);
      }
    };

    const checkFilterSpace = () => {
      const now = Date.now();

      if (shouldSkipSpaceCheck(now)) {
        return;
      }

      if (shouldHideFiltersForOverlays()) {
        if (showAllFilters) {
          lastStateChange = now;
          setShowAllFilters(false);
        }
        return;
      }

      const toolbarContainer = getToolbarContainer();
      if (!toolbarContainer) {
        return;
      }

      const availableSpace = calculateAvailableSpace(toolbarContainer, isSidePaneOpen);
      const minimumFilterSpace = 500;
      const hasSpaceForInlineFilters = availableSpace >= minimumFilterSpace;

      updateFiltersVisibility(hasSpaceForInlineFilters, now);
    };

    const debouncedCheckFilterSpace = () => {
      if (debounceTimeout) {
        clearTimeout(debounceTimeout);
      }
      debounceTimeout = setTimeout(checkFilterSpace, 100);
    };

    checkFilterSpace();

    const handleResize = () => {
      debouncedCheckFilterSpace();
    };

    const handleSideMenuToggle = () => {
      debouncedCheckFilterSpace();
    };

    const handleSideMenuResize = () => {
      debouncedCheckFilterSpace();
    };

    const timeoutId = setTimeout(() => {
      forceUpdate({});
      checkFilterSpace();
    }, 100);

    window.addEventListener("resize", handleResize);
    window.addEventListener("side-menu-toggle", handleSideMenuToggle);
    window.addEventListener("side-menu-resize", handleSideMenuResize);

    return () => {
      window.removeEventListener("resize", handleResize);
      window.removeEventListener("side-menu-toggle", handleSideMenuToggle);
      window.removeEventListener("side-menu-resize", handleSideMenuResize);
      clearTimeout(timeoutId);
      if (debounceTimeout) {
        clearTimeout(debounceTimeout);
      }
    };
  }, [activeFilterCount, showAllFilters, isMobileMenuOpen, isOverlayOpen, isSidePaneOpen]);

  useEffect(() => {
    const is2XlScreen = window.matchMedia("(min-width: 1536px)").matches;
    const isMobileScreen = window.matchMedia("(max-width: 639px)").matches;
    const shouldUseCompactButtons = (!is2XlScreen && (showAllFilters || activeFilterCount > 0)) || isMobileScreen;

    onFilterStateChange?.(showAllFilters, activeFilterCount > 0, shouldUseCompactButtons);
  }, [showAllFilters, activeFilterCount, onFilterStateChange]);

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
        className="w-60 shrink-0"
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

                if (isOverlayOpen || isMobileMenuOpen) {
                  setIsFilterPanelOpen(true);
                  return;
                }

                if (!containerRef.current) {
                  setIsFilterPanelOpen(true);
                  return;
                }

                const toolbarContainer = containerRef.current.closest(
                  ".flex.items-center.justify-between"
                ) as HTMLElement;
                if (!toolbarContainer) {
                  setIsFilterPanelOpen(true);
                  return;
                }

                const toolbarWidth = toolbarContainer.offsetWidth;
                const searchField = containerRef.current.querySelector('input[type="text"]') as HTMLElement;
                const filterButton = containerRef.current.querySelector('[data-testid="filter-button"]') as HTMLElement;

                const searchWidth = searchField?.offsetWidth || 300;
                const filterButtonWidth = filterButton?.offsetWidth || 50;

                const rightSideWidth = 130;

                const gaps = 16;
                const minimumFilterSpace = 500;

                const usedSpace = searchWidth + filterButtonWidth + rightSideWidth + gaps;
                const availableSpace = toolbarWidth - usedSpace;

                const hasSpaceForInlineFilters = availableSpace >= minimumFilterSpace;

                if (hasSpaceForInlineFilters) {
                  setShowAllFilters(!showAllFilters);
                  return;
                }
                setIsFilterPanelOpen(true);
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
