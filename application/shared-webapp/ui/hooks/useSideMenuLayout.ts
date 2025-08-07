import type React from "react";
import { useEffect, useState } from "react";
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

  // Helper to get initial collapsed state
  const getInitialCollapsed = (isSmall: boolean, isLarge: boolean) => {
    // Force collapsed on medium screens
    if (isSmall && !isLarge) {
      return true;
    }
    // Check localStorage for large screens
    return localStorage.getItem("side-menu-collapsed") === "true";
  };

  // Track menu state
  const [isCollapsed, setIsCollapsed] = useState(() => getInitialCollapsed(isSmallScreen, isLargeScreen));
  const [isOverlayExpanded, setIsOverlayExpanded] = useState(false);
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);
  const [customMenuWidth, setCustomMenuWidth] = useState(() => {
    const stored = localStorage.getItem("side-menu-size");
    const width = stored ? Number.parseInt(stored, 10) : Number.NaN;
    return width >= SIDE_MENU_MIN_WIDTH && width <= SIDE_MENU_MAX_WIDTH ? width : SIDE_MENU_DEFAULT_WIDTH;
  });

  // Listen for screen size changes and menu events
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

    // Custom event handlers
    const handleMenuToggle = (event: Event) => {
      setIsCollapsed((event as CustomEvent).detail.isCollapsed);
    };
    const handleOverlayToggle = (event: Event) => {
      setIsOverlayExpanded((event as CustomEvent).detail.isExpanded);
    };
    const handleMobileMenuToggle = (event: Event) => {
      setIsMobileMenuOpen((event as CustomEvent).detail.isOpen);
    };
    const handleMenuResize = (event: Event) => {
      setCustomMenuWidth((event as CustomEvent).detail.width);
    };

    // Add all listeners
    smQuery.addEventListener("change", handleSmChange);
    xlQuery.addEventListener("change", handleXlChange);
    window.addEventListener("side-menu-toggle", handleMenuToggle);
    window.addEventListener("side-menu-overlay-toggle", handleOverlayToggle);
    window.addEventListener("mobile-menu-toggle", handleMobileMenuToggle);
    window.addEventListener("side-menu-resize", handleMenuResize);

    return () => {
      smQuery.removeEventListener("change", handleSmChange);
      xlQuery.removeEventListener("change", handleXlChange);
      window.removeEventListener("side-menu-toggle", handleMenuToggle);
      window.removeEventListener("side-menu-overlay-toggle", handleOverlayToggle);
      window.removeEventListener("mobile-menu-toggle", handleMobileMenuToggle);
      window.removeEventListener("side-menu-resize", handleMenuResize);
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

  // Calculate layout styles (simple enough not to need memoization)
  const style: React.CSSProperties = !isSmallScreen
    ? {} // Mobile: full width
    : isSmallScreen && !isLargeScreen
      ? { marginLeft: `${SIDE_MENU_COLLAPSED_WIDTH}px` } // Medium screens (overlay mode)
      : { marginLeft: isCollapsed ? `${SIDE_MENU_COLLAPSED_WIDTH}px` : `${customMenuWidth}px` }; // Large screens

  // Derive overlay state
  const isOverlayMode = isSmallScreen && !isLargeScreen;
  const isOverlayOpen = isOverlayMode && isOverlayExpanded;

  return {
    className,
    style,
    isOverlayOpen,
    isMobileMenuOpen,
    isCollapsed: isLargeScreen ? isCollapsed : true, // For XL screens, return actual state; for others, consider "collapsed" for space calculation
    isLargeScreen
  };
}
