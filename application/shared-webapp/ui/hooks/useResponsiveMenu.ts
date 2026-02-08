import { useEffect, useState } from "react";
import { MEDIA_QUERIES, SIDE_MENU_DEFAULT_WIDTH_REM } from "../utils/responsive";

// Expanded menu width in rem
const EXPANDED_MENU_WIDTH_REM = SIDE_MENU_DEFAULT_WIDTH_REM;

// Minimum comfortable content width when menu is expanded (in rem)
// Based on old xl breakpoint (1280px): content area was ~62rem (1280-288=992px, 992/16=62rem)
const MIN_CONTENT_WIDTH_REM = 62;

/**
 * Hook to determine responsive menu behavior
 * - Hidden: below sm breakpoint (< 640px)
 * - Overlay mode: not enough space for expanded menu + comfortable content
 * - Force collapsed: when in overlay mode
 * @returns Configuration object for responsive menu behavior
 */
export function useResponsiveMenu() {
  const [isAboveSm, setIsAboveSm] = useState(false);
  const [hasSpaceForExpanded, setHasSpaceForExpanded] = useState(true);

  useEffect(() => {
    const checkLayout = () => {
      // Check if above sm breakpoint (640px+)
      const smMatches = window.matchMedia(MEDIA_QUERIES.sm).matches;
      setIsAboveSm(smMatches);

      // Calculate if there's room for expanded menu + comfortable content
      const rootFontSize = parseFloat(getComputedStyle(document.documentElement).fontSize);
      const expandedMenuWidth = EXPANDED_MENU_WIDTH_REM * rootFontSize;
      const minContentWidth = MIN_CONTENT_WIDTH_REM * rootFontSize;

      // Need space for menu + minimum content width
      const minViewportForExpanded = expandedMenuWidth + minContentWidth;
      setHasSpaceForExpanded(window.innerWidth >= minViewportForExpanded);
    };

    checkLayout();
    window.addEventListener("resize", checkLayout);
    return () => window.removeEventListener("resize", checkLayout);
  }, []);

  // Menu behavior logic
  const isHidden = !isAboveSm; // < 640px: hidden
  const overlayMode = isAboveSm && !hasSpaceForExpanded; // 640px+ but not enough space: overlay
  const forceCollapsed = overlayMode; // Force collapsed in overlay mode
  const className = isHidden ? "hidden" : "flex";

  return { className, forceCollapsed, overlayMode, isHidden };
}
