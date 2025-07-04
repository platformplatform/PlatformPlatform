import type React from "react";
import { useEffect, useMemo, useState } from "react";
import {
  MEDIA_QUERIES,
  SIDE_MENU_COLLAPSED_WIDTH,
  SIDE_MENU_DEFAULT_WIDTH,
  SIDE_MENU_MAX_WIDTH,
  SIDE_MENU_MIN_WIDTH
} from "../utils/responsive";

/**
 * Hook to provide proper layout styles for content when using a fixed side menu.
 * Listens to side menu toggle events and screen size changes to calculate
 * the correct margin and width to prevent content from being hidden behind the menu.
 */
export function useSideMenuLayout(): {
  className: string;
  style: React.CSSProperties;
  isOverlayOpen: boolean;
  isMobileMenuOpen: boolean;
  isCollapsed: boolean;
  isLargeScreen: boolean;
} {
  // Track screen sizes
  const [isSmallScreen, setIsSmallScreen] = useState(() => window.matchMedia(MEDIA_QUERIES.sm).matches);
  const [isLargeScreen, setIsLargeScreen] = useState(() => window.matchMedia(MEDIA_QUERIES.xl).matches);

  // Track menu state
  const [isCollapsed, setIsCollapsed] = useState(() => {
    // Synchronous check to prevent flicker
    const isSmallScreenSync = window.matchMedia(MEDIA_QUERIES.sm).matches;
    const isLargeScreenSync = window.matchMedia(MEDIA_QUERIES.xl).matches;

    // Force collapsed on medium screens
    if (isSmallScreenSync && !isLargeScreenSync) {
      return true;
    }

    // Check localStorage for large screens
    const stored = localStorage.getItem("side-menu-collapsed");
    return stored === "true";
  });

  const [isOverlayExpanded, setIsOverlayExpanded] = useState(false);
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);
  const [customMenuWidth, setCustomMenuWidth] = useState(() => {
    try {
      const stored = localStorage.getItem("side-menu-size");
      if (stored) {
        const width = Number.parseInt(stored, 10);
        if (!Number.isNaN(width) && width >= SIDE_MENU_MIN_WIDTH && width <= SIDE_MENU_MAX_WIDTH) {
          return width;
        }
      }
    } catch {}
    return SIDE_MENU_DEFAULT_WIDTH;
  });

  // Listen for screen size changes
  useEffect(() => {
    const smQuery = window.matchMedia(MEDIA_QUERIES.sm);
    const xlQuery = window.matchMedia(MEDIA_QUERIES.xl);

    const handleSmChange = (e: MediaQueryListEvent) => setIsSmallScreen(e.matches);
    const handleXlChange = (e: MediaQueryListEvent) => {
      setIsLargeScreen(e.matches);
      // When transitioning to XL screen, sync collapsed state from localStorage
      if (e.matches) {
        const stored = localStorage.getItem("side-menu-collapsed");
        setIsCollapsed(stored === "true");
      }
    };

    smQuery.addEventListener("change", handleSmChange);
    xlQuery.addEventListener("change", handleXlChange);

    return () => {
      smQuery.removeEventListener("change", handleSmChange);
      xlQuery.removeEventListener("change", handleXlChange);
    };
  }, []);

  // Listen for menu state changes via custom events
  useEffect(() => {
    const handleMenuToggle = (event: CustomEvent) => {
      setIsCollapsed(event.detail.isCollapsed);
    };

    const handleOverlayToggle = (event: CustomEvent) => {
      setIsOverlayExpanded(event.detail.isExpanded);
    };

    const handleMobileMenuToggle = (event: CustomEvent) => {
      setIsMobileMenuOpen(event.detail.isOpen);
    };

    const handleMenuResize = (event: CustomEvent) => {
      setCustomMenuWidth(event.detail.width);
    };

    window.addEventListener("side-menu-toggle", handleMenuToggle as EventListener);
    window.addEventListener("side-menu-overlay-toggle", handleOverlayToggle as EventListener);
    window.addEventListener("mobile-menu-toggle", handleMobileMenuToggle as EventListener);
    window.addEventListener("side-menu-resize", handleMenuResize as EventListener);

    return () => {
      window.removeEventListener("side-menu-toggle", handleMenuToggle as EventListener);
      window.removeEventListener("side-menu-overlay-toggle", handleOverlayToggle as EventListener);
      window.removeEventListener("mobile-menu-toggle", handleMobileMenuToggle as EventListener);
      window.removeEventListener("side-menu-resize", handleMenuResize as EventListener);
    };
  }, []);

  // Reset overlay expanded state when leaving overlay mode
  useEffect(() => {
    if (isLargeScreen) {
      setIsOverlayExpanded(false);
    }
  }, [isLargeScreen]);

  // Calculate layout styles
  const className = "flex flex-col flex-1 min-h-0";

  const style = useMemo((): React.CSSProperties => {
    // Mobile: full width
    if (!isSmallScreen) {
      return {};
    }

    // Medium screens (overlay mode): always use collapsed width
    if (isSmallScreen && !isLargeScreen) {
      return {
        marginLeft: `${SIDE_MENU_COLLAPSED_WIDTH}px`
      };
    }

    // Large screens: adjust based on menu state
    return {
      marginLeft: isCollapsed ? `${SIDE_MENU_COLLAPSED_WIDTH}px` : `${customMenuWidth}px`
    };
  }, [isSmallScreen, isLargeScreen, isCollapsed, customMenuWidth]);

  // Determine if in overlay mode
  const isOverlayMode = isSmallScreen && !isLargeScreen;

  return {
    className,
    style,
    isOverlayOpen: isOverlayMode && isOverlayExpanded,
    isMobileMenuOpen,
    isCollapsed: isLargeScreen ? isCollapsed : true, // For XL screens, return actual state; for others, consider "collapsed" for space calculation
    isLargeScreen
  };
}
