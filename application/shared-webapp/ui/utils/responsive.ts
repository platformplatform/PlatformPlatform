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

// Media query strings matching Tailwind breakpoints
export const MEDIA_QUERIES = {
  sm: "(min-width: 640px)",
  md: "(min-width: 768px)",
  lg: "(min-width: 1024px)",
  xl: "(min-width: 1280px)",
  "2xl": "(min-width: 1536px)"
} as const;

// Side menu width constants
export const SIDE_MENU_COLLAPSED_WIDTH = 72;
export const SIDE_MENU_MIN_WIDTH = 150;
export const SIDE_MENU_MAX_WIDTH = 350;
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
