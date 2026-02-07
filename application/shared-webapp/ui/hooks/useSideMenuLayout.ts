import type React from "react";
import { useEffect, useState } from "react";
import {
  getRootFontSize,
  getSideMenuCollapsedWidth,
  MEDIA_QUERIES,
  SIDE_MENU_DEFAULT_WIDTH_REM,
  SIDE_MENU_MAX_WIDTH_REM,
  SIDE_MENU_MIN_WIDTH_REM
} from "../utils/responsive";

// Minimum content width in rem (matches useResponsiveMenu logic)
const MIN_CONTENT_WIDTH_REM = 62;

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
  const [hasSpaceForExpanded, setHasSpaceForExpanded] = useState(true);

  // Helper to get initial collapsed state
  const getInitialCollapsed = (isSmall: boolean, hasSpace: boolean) => {
    // Force collapsed when no space for expanded menu
    if (isSmall && !hasSpace) {
      return true;
    }
    // Check localStorage when there's space
    return localStorage.getItem("side-menu-collapsed") === "true";
  };

  // Track menu state
  const [isCollapsed, setIsCollapsed] = useState(() => getInitialCollapsed(isSmallScreen, hasSpaceForExpanded));
  const [isOverlayExpanded, setIsOverlayExpanded] = useState(false);
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);
  const [customMenuWidthRem, setCustomMenuWidthRem] = useState(() => {
    const stored = localStorage.getItem("side-menu-size");
    const width = stored ? Number.parseFloat(stored) : Number.NaN;
    return width >= SIDE_MENU_MIN_WIDTH_REM && width <= SIDE_MENU_MAX_WIDTH_REM ? width : SIDE_MENU_DEFAULT_WIDTH_REM;
  });

  // Listen for screen size changes and menu events
  useEffect(() => {
    const smQuery = window.matchMedia(MEDIA_QUERIES.sm);

    const handleSmChange = (e: MediaQueryListEvent) => setIsSmallScreen(e.matches);

    const checkSpace = () => {
      const rootFontSize = getRootFontSize();
      const expandedMenuWidth = SIDE_MENU_DEFAULT_WIDTH_REM * rootFontSize;
      const minContentWidth = MIN_CONTENT_WIDTH_REM * rootFontSize;
      const minViewportForExpanded = expandedMenuWidth + minContentWidth;

      const hasSpace = window.innerWidth >= minViewportForExpanded;
      setHasSpaceForExpanded(hasSpace);

      // When transitioning to expanded mode, sync collapsed state from localStorage
      if (hasSpace) {
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
      setCustomMenuWidthRem((event as CustomEvent).detail.widthRem);
    };

    // Add all listeners
    smQuery.addEventListener("change", handleSmChange);
    window.addEventListener("resize", checkSpace);
    window.addEventListener("side-menu-toggle", handleMenuToggle);
    window.addEventListener("side-menu-overlay-toggle", handleOverlayToggle);
    window.addEventListener("mobile-menu-toggle", handleMobileMenuToggle);
    window.addEventListener("side-menu-resize", handleMenuResize);

    // Initial check
    checkSpace();

    return () => {
      smQuery.removeEventListener("change", handleSmChange);
      window.removeEventListener("resize", checkSpace);
      window.removeEventListener("side-menu-toggle", handleMenuToggle);
      window.removeEventListener("side-menu-overlay-toggle", handleOverlayToggle);
      window.removeEventListener("mobile-menu-toggle", handleMobileMenuToggle);
      window.removeEventListener("side-menu-resize", handleMenuResize);
    };
  }, []);

  // Reset overlay expanded state when entering expanded mode
  useEffect(() => {
    if (hasSpaceForExpanded) {
      setIsOverlayExpanded(false);
    }
  }, [hasSpaceForExpanded]);

  // Calculate layout styles
  const className = "flex flex-col flex-1 min-h-0";

  // Calculate layout styles (simple enough not to need memoization)
  // Use the CSS variable value for collapsed width since it scales with font size
  const collapsedWidth = getSideMenuCollapsedWidth();
  const style: React.CSSProperties = !isSmallScreen
    ? {} // Mobile: full width
    : isSmallScreen && !hasSpaceForExpanded
      ? { marginLeft: `${collapsedWidth}px` } // Overlay mode (force collapsed)
      : { marginLeft: isCollapsed ? `${collapsedWidth}px` : `${customMenuWidthRem}rem` }; // Expanded mode

  // Derive overlay state
  const isOverlayMode = isSmallScreen && !hasSpaceForExpanded;
  const isOverlayOpen = isOverlayMode && isOverlayExpanded;

  return {
    className,
    style,
    isOverlayOpen,
    isMobileMenuOpen,
    isCollapsed: hasSpaceForExpanded ? isCollapsed : true,
    isLargeScreen: hasSpaceForExpanded
  };
}
