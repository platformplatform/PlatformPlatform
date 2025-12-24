import { parseDate } from "@internationalized/date";
import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { DateRangePicker } from "@repo/ui/components/DateRangePicker";
import { Dialog } from "@repo/ui/components/Dialog";
import { DialogContent, DialogFooter, DialogHeader } from "@repo/ui/components/DialogFooter";
import { Heading } from "@repo/ui/components/Heading";
import { Modal } from "@repo/ui/components/Modal";
import { SearchField } from "@repo/ui/components/SearchField";
import { Select, SelectItem } from "@repo/ui/components/Select";
import { Tooltip, TooltipTrigger } from "@repo/ui/components/Tooltip";
import { useDebounce } from "@repo/ui/hooks/useDebounce";
import { useSideMenuLayout } from "@repo/ui/hooks/useSideMenuLayout";
import { useLocation, useNavigate } from "@tanstack/react-router";
import { Filter, FilterX, XIcon } from "lucide-react";
import { useCallback, useEffect, useRef, useState } from "react";
import { type SortableUserProperties, type SortOrder, UserRole, UserStatus } from "@/shared/lib/api/client";
import { getUserRoleLabel } from "@/shared/lib/api/userRole";
import { getUserStatusLabel } from "@/shared/lib/api/userStatus";

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

interface UserQueryingProps {
  onFilterStateChange?: (
    isFilterBarExpanded: boolean,
    hasActiveFilters: boolean,
    shouldUseCompactButtons: boolean
  ) => void;
  onFiltersUpdated?: () => void;
}

/**
 * UserQuerying component handles the user list filtering.
 * Uses URL parameters as the single source of truth for all filters.
 * The only local state is for the search input, which is debounced
 * to prevent too many URL updates while typing.
 */
export function UserQuerying({ onFilterStateChange, onFiltersUpdated }: UserQueryingProps = {}) {
  const navigate = useNavigate();
  const searchParams = (useLocation().search as SearchParams) ?? {};
  const { isOverlayOpen, isMobileMenuOpen } = useSideMenuLayout();
  const containerRef = useRef<HTMLDivElement>(null);
  const [search, setSearch] = useState<string | undefined>(searchParams.search);
  const debouncedSearch = useDebounce(search, 500);
  const [showAllFilters, setShowAllFilters] = useState(
    Boolean(searchParams.userRole ?? searchParams.userStatus ?? searchParams.startDate ?? searchParams.endDate)
  );
  const [isFilterPanelOpen, setIsFilterPanelOpen] = useState(false);
  const [, forceUpdate] = useState({});

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
    (params: Partial<SearchParams>, isSearchUpdate = false) => {
      navigate({
        to: "/admin/users",
        search: (prev) => ({
          ...prev,
          ...params,
          pageOffset: undefined
        })
      });
      // Only call onFiltersUpdated for actual filter changes, not search updates
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

  // Detect if side pane is open by checking DOM
  const [isSidePaneOpen, setIsSidePaneOpen] = useState(false);

  useEffect(() => {
    const checkSidePaneState = () => {
      const sidePane = document.querySelector('[class*="fixed"][class*="inset-0"][class*="z-70"]');
      const isOpen = !!sidePane;
      if (isOpen !== isSidePaneOpen) {
        setIsSidePaneOpen(isOpen);
      }
    };

    // Check immediately
    checkSidePaneState();

    // Use MutationObserver to detect when side pane is added/removed
    const observer = new MutationObserver(checkSidePaneState);
    observer.observe(document.body, { childList: true, subtree: true });

    return () => observer.disconnect();
  }, [isSidePaneOpen]);

  // Handle screen size and container space changes to show/hide filters appropriately
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
      // Account for invite button and potential side panel
      const rightSideWidth = sidePaneOpen ? 200 : 150;
      const gaps = 24; // Increased gap allowance

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

    // Run check immediately
    checkFilterSpace();

    // Also listen for resize events to handle browser-specific timing issues
    const handleResize = () => {
      debouncedCheckFilterSpace();
    };

    // Listen for side menu events that affect layout
    const handleSideMenuToggle = () => {
      debouncedCheckFilterSpace();
    };

    const handleSideMenuResize = () => {
      debouncedCheckFilterSpace();
    };

    // Force a recheck after mount to ensure correct initial state across browsers
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

  // Notify parent component when filter state changes
  useEffect(() => {
    // On 2XL+ screens, keep full buttons even with filters
    const is2XlScreen = window.matchMedia("(min-width: 1536px)").matches;
    const isMobileScreen = window.matchMedia("(max-width: 639px)").matches; // sm breakpoint
    const shouldUseCompactButtons = (!is2XlScreen && (showAllFilters || activeFilterCount > 0)) || isMobileScreen;

    onFilterStateChange?.(showAllFilters, activeFilterCount > 0, shouldUseCompactButtons);
  }, [showAllFilters, activeFilterCount, onFilterStateChange]);

  const clearAllFilters = () => {
    // Set search to empty string first to ensure UI updates immediately
    setSearch("");

    // Then update the filter which will set search to undefined in URL
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
      <SearchField placeholder={t`Search`} value={search} onChange={setSearch} label={t`Search`} className="min-w-32" />

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
      <TooltipTrigger>
        <Button
          variant="secondary"
          className="relative mt-6"
          aria-label={showAllFilters ? t`Clear filters` : t`Show filters`}
          data-testid="filter-button"
          onClick={() => {
            // If filters are currently showing and user clicks, always clear filters
            // This ensures consistent behavior regardless of space calculations
            if (showAllFilters) {
              clearAllFilters();
              return;
            }

            // Force modal when overlays are open (blocks interaction)
            if (isOverlayOpen || isMobileMenuOpen) {
              setIsFilterPanelOpen(true);
              return;
            }

            if (!containerRef.current) {
              setIsFilterPanelOpen(true);
              return;
            }

            // Measure the actual available space by finding the parent toolbar container
            const toolbarContainer = containerRef.current.closest(".flex.items-center.justify-between") as HTMLElement;
            if (!toolbarContainer) {
              setIsFilterPanelOpen(true);
              return;
            }

            const toolbarWidth = toolbarContainer.offsetWidth;
            const searchField = containerRef.current.querySelector('input[type="text"]') as HTMLElement;
            const filterButton = containerRef.current.querySelector('[data-testid="filter-button"]') as HTMLElement;

            // Calculate space used by existing elements - ALWAYS assume filters are hidden for measurement
            const searchWidth = searchField?.offsetWidth || 300;
            const filterButtonWidth = filterButton?.offsetWidth || 50;

            // For space calculation, assume buttons will be compact (130px) when filters are shown
            // This accounts for the fact that showing filters makes buttons compact, freeing up space
            const rightSideWidth = 130;

            const gaps = 16; // gap-2 between main sections
            const minimumFilterSpace = 500;

            const usedSpace = searchWidth + filterButtonWidth + rightSideWidth + gaps;
            const availableSpace = toolbarWidth - usedSpace;

            const hasSpaceForInlineFilters = availableSpace >= minimumFilterSpace;

            if (hasSpaceForInlineFilters) {
              // If we have space but filters aren't showing, toggle them
              setShowAllFilters(!showAllFilters);
              return;
            }
            // If we don't have space, open dialog
            setIsFilterPanelOpen(true);
          }}
        >
          {showAllFilters ? (
            <FilterX size={16} aria-label={t`Clear filters`} />
          ) : (
            <Filter size={16} aria-label={t`Show filters`} />
          )}
          {activeFilterCount > 0 && !showAllFilters && (
            <span className="absolute -top-1 -right-1 flex h-5 w-5 items-center justify-center rounded-full bg-primary font-medium text-primary-foreground text-xs">
              {activeFilterCount}
            </span>
          )}
        </Button>
        <Tooltip>{showAllFilters ? <Trans>Clear filters</Trans> : <Trans>Show filters</Trans>}</Tooltip>
      </TooltipTrigger>

      {/* Filter dialog for small/medium screens */}
      <Modal isOpen={isFilterPanelOpen} onOpenChange={setIsFilterPanelOpen} isDismissable={true}>
        <Dialog className="w-full sm:min-w-[400px]">
          <XIcon
            onClick={() => setIsFilterPanelOpen(false)}
            className="absolute top-2 right-2 h-10 w-10 p-2 hover:bg-muted"
          />
          <DialogHeader>
            <Heading slot="title" className="text-2xl">
              <Trans>Filters</Trans>
            </Heading>
          </DialogHeader>

          <DialogContent className="flex flex-col gap-4">
            <SearchField
              placeholder={t`Search`}
              value={search}
              onChange={setSearch}
              label={t`Search`}
              className="w-full"
            />

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
          </DialogContent>

          <DialogFooter>
            <Button
              variant="secondary"
              onClick={clearAllFilters}
              disabled={activeFilterCount === 0 && !searchParams.search}
            >
              <Trans>Clear</Trans>
            </Button>
            <Button variant="default" onClick={() => setIsFilterPanelOpen(false)}>
              <Trans>OK</Trans>
            </Button>
          </DialogFooter>
        </Dialog>
      </Modal>
    </div>
  );
}
