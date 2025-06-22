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

// Side menu width constants (including 8px right margin)
export const SIDE_MENU_COLLAPSED_WIDTH = "80px"; // 72px + 8px margin
export const SIDE_MENU_EXPANDED_WIDTH = "296px"; // 288px + 8px margin
