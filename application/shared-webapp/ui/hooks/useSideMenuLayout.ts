import type React from "react";
import { useEffect, useMemo, useState } from "react";
import { MEDIA_QUERIES, SIDE_MENU_COLLAPSED_WIDTH, SIDE_MENU_EXPANDED_WIDTH } from "../utils/responsive";

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
} {
  // Track screen sizes
  const [isSmallScreen, setIsSmallScreen] = useState(() =>
    typeof window !== "undefined" ? window.matchMedia(MEDIA_QUERIES.sm).matches : false
  );
  const [isLargeScreen, setIsLargeScreen] = useState(() =>
    typeof window !== "undefined" ? window.matchMedia(MEDIA_QUERIES.xl).matches : false
  );

  // Track menu state
  const [isCollapsed, setIsCollapsed] = useState(() => {
    // Synchronous check to prevent flicker
    if (typeof window === "undefined") {
      return true;
    }

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

  // Listen for screen size changes
  useEffect(() => {
    const smQuery = window.matchMedia(MEDIA_QUERIES.sm);
    const xlQuery = window.matchMedia(MEDIA_QUERIES.xl);

    const handleSmChange = (e: MediaQueryListEvent) => setIsSmallScreen(e.matches);
    const handleXlChange = (e: MediaQueryListEvent) => setIsLargeScreen(e.matches);

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

    window.addEventListener("side-menu-toggle", handleMenuToggle as EventListener);
    window.addEventListener("side-menu-overlay-toggle", handleOverlayToggle as EventListener);
    window.addEventListener("mobile-menu-toggle", handleMobileMenuToggle as EventListener);

    return () => {
      window.removeEventListener("side-menu-toggle", handleMenuToggle as EventListener);
      window.removeEventListener("side-menu-overlay-toggle", handleOverlayToggle as EventListener);
      window.removeEventListener("mobile-menu-toggle", handleMobileMenuToggle as EventListener);
    };
  }, []);

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
        marginLeft: SIDE_MENU_COLLAPSED_WIDTH
      };
    }

    // Large screens: adjust based on menu state
    return {
      marginLeft: isCollapsed ? SIDE_MENU_COLLAPSED_WIDTH : SIDE_MENU_EXPANDED_WIDTH
    };
  }, [isSmallScreen, isLargeScreen, isCollapsed]);

  // Determine if in overlay mode
  const isOverlayMode = isSmallScreen && !isLargeScreen;

  return { className, style, isOverlayOpen: isOverlayMode && isOverlayExpanded, isMobileMenuOpen };
}
