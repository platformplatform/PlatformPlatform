import { trackInteraction } from "@repo/infrastructure/applicationInsights/ApplicationInsightsProvider";
import { parseDateString } from "@repo/ui/components/DateRangePicker";
import { useDebounce } from "@repo/ui/hooks/useDebounce";
import { useSideMenuLayout } from "@repo/ui/hooks/useSideMenuLayout";
import { useLocation, useNavigate } from "@tanstack/react-router";
import { useCallback, useEffect, useRef, useState } from "react";

import type { SearchParams } from "./userQueryingTypes";

const THRESHOLD_FILTERS_EXPANDED_REM = 55.75;

function getRemInPixels(): number {
  return parseFloat(getComputedStyle(document.documentElement).fontSize);
}

function hasSpaceForInlineFilters(toolbarWidth: number): boolean {
  const threshold = THRESHOLD_FILTERS_EXPANDED_REM * getRemInPixels();
  return toolbarWidth >= threshold;
}

interface UseUserFiltersOptions {
  onFiltersUpdated?: () => void;
  onFiltersExpandedChange?: (expanded: boolean) => void;
}

export function useUserFilters({ onFiltersUpdated, onFiltersExpandedChange }: UseUserFiltersOptions = {}) {
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
      ? { start: parseDateString(searchParams.startDate), end: parseDateString(searchParams.endDate) }
      : null;

  const updateFilter = useCallback(
    (params: Partial<SearchParams>, isSearchUpdate = false) => {
      navigate({
        to: "/account/users",
        search: (prev) => ({ ...prev, ...params, pageOffset: undefined })
      });
      if (!isSearchUpdate) {
        onFiltersUpdated?.();
      }
    },
    [navigate, onFiltersUpdated]
  );

  useEffect(() => {
    navigate({
      to: "/account/users",
      search: (prev) => ({ ...prev, search: debouncedSearch || undefined, pageOffset: undefined })
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

  useEffect(() => {
    onFiltersExpandedChange?.(showAllFilters);
  }, [showAllFilters, onFiltersExpandedChange]);

  const clearAllFilters = () => {
    trackInteraction("User filters", "interaction", "Clear");
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

  const handleFilterButtonClick = () => {
    if (showAllFilters) {
      clearAllFilters();
      return;
    }
    const toolbar = containerRef.current?.closest(".flex.items-center.justify-between") as HTMLElement;
    if (!isOverlayOpen && !isMobileMenuOpen && toolbar && hasSpaceForInlineFilters(toolbar.offsetWidth)) {
      setShowAllFilters(true);
      trackInteraction("User filters", "interaction", "Expand");
    } else {
      setIsFilterPanelOpen(true);
    }
  };

  return {
    containerRef,
    search,
    setSearch,
    searchParams,
    dateRange,
    showAllFilters,
    isFilterPanelOpen,
    setIsFilterPanelOpen,
    activeFilterCount,
    updateFilter,
    clearAllFilters,
    handleFilterButtonClick
  };
}
