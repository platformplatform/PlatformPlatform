/**
 * Centralized responsive breakpoint utilities
 * Uses Tailwind's default breakpoint values
 */
export const BREAKPOINTS = {
  sm: "sm", // 640px
  md: "md", // 768px
  lg: "lg", // 1024px
  xl: "xl", // 1280px
  "2xl": "2xl" // 1536px
} as const;

// Breakpoint pixel values matching Tailwind defaults
export const BREAKPOINT_PX = {
  sm: 640,
  md: 768,
  lg: 1024,
  xl: 1280,
  "2xl": 1536
} as const;

// Media query strings matching Tailwind breakpoints
export const MEDIA_QUERIES = {
  sm: `(min-width: ${BREAKPOINT_PX.sm}px)`,
  md: `(min-width: ${BREAKPOINT_PX.md}px)`,
  lg: `(min-width: ${BREAKPOINT_PX.lg}px)`,
  xl: `(min-width: ${BREAKPOINT_PX.xl}px)`,
  "2xl": `(min-width: ${BREAKPOINT_PX["2xl"]}px)`
} as const;

// Side menu width constants
// Collapsed width is 5rem - compute pixel value dynamically
export const SIDE_MENU_COLLAPSED_WIDTH_REM = 5;
export function getSideMenuCollapsedWidth(): number {
  const rootFontSize = parseFloat(getComputedStyle(document.documentElement).fontSize);
  return SIDE_MENU_COLLAPSED_WIDTH_REM * rootFontSize;
}
export const SIDE_MENU_MIN_WIDTH = 150;
export const SIDE_MENU_MAX_WIDTH = 500;
export const SIDE_MENU_DEFAULT_WIDTH = 288;

// Helper function to detect touch devices (including iPads)
export function isTouchDevice(): boolean {
  return "ontouchstart" in window || navigator.maxTouchPoints > 0;
}

// Viewport detection helpers - based on Tailwind breakpoints
// Mobile: below 640px (sm)
export function isMobileViewport(): boolean {
  return !window.matchMedia(MEDIA_QUERIES.sm).matches;
}

// Small viewport and up: 640px+ (sm)
export function isSmallViewportOrLarger(): boolean {
  return window.matchMedia(MEDIA_QUERIES.sm).matches;
}

// Medium viewport and up: 768px+ (md)
export function isMediumViewportOrLarger(): boolean {
  return window.matchMedia(MEDIA_QUERIES.md).matches;
}

// Large viewport and up: 1024px+ (lg)
export function isLargeViewportOrLarger(): boolean {
  return window.matchMedia(MEDIA_QUERIES.lg).matches;
}

// Extra large viewport and up: 1280px+ (xl)
export function isExtraLargeViewportOrLarger(): boolean {
  return window.matchMedia(MEDIA_QUERIES.xl).matches;
}
