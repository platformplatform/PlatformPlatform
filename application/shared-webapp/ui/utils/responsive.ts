/**
 * Centralized responsive breakpoint utilities
 * Uses Tailwind's default breakpoint values
 */
export const BREAKPOINTS = {
  sm: "sm", // 640px
  xl: "xl" // 1280px
} as const;

// Media query strings matching Tailwind breakpoints
export const MEDIA_QUERIES = {
  sm: "(min-width: 640px)",
  xl: "(min-width: 1280px)"
} as const;

// Side menu width constants
export const SIDE_MENU_COLLAPSED_WIDTH = "72px";
export const SIDE_MENU_EXPANDED_WIDTH = "288px"; // w-72 in Tailwind
