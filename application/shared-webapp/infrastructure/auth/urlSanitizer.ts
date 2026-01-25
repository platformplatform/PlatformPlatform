/**
 * URL sanitization utility for cross-tenant navigation
 *
 * Removes entity-specific IDs and query parameters from URLs to prevent
 * 404 errors when switching between tenants where entities don't exist
 */

/**
 * Pattern to match PlatformPlatform entity IDs
 * Format: prefix_26CHARACTERS where prefix is 2-5 lowercase letters
 * Examples: usr_01K1FYBDQ2NSPQ4NPJQ8Q5XCNY, eng_01JSNZ5NBF6RRBE2GK1K2PY0WM
 */
const ENTITY_ID_PATTERN = /[a-z]{2,5}_[A-Z0-9]{26}/;

/**
 * Sanitizes a URL by removing entity-specific IDs and all query parameters
 *
 * @param url The URL to sanitize
 * @returns The sanitized URL safe for cross-tenant navigation
 *
 * @example
 * sanitizeUrl('/account/users?userId=usr_01K1FYBDQ2NSPQ4NPJQ8Q5XCNY')
 * // Returns: '/account/users'
 */
export function sanitizeUrl(url: string): string {
  try {
    // Parse the URL
    const urlObj = new URL(url, window.location.origin);

    // Remove all query parameters
    urlObj.search = "";

    // Remove entity IDs from the pathname
    let pathname = urlObj.pathname;

    // Remove entity IDs that appear as path segments
    pathname = pathname
      .split("/")
      .filter((segment) => {
        return !ENTITY_ID_PATTERN.test(segment);
      })
      .join("/");

    // Ensure we don't end up with double slashes
    pathname = pathname.replace(/\/+/g, "/");

    // Ensure we don't lose the root slash
    if (!pathname.startsWith("/")) {
      pathname = `/${pathname}`;
    }

    urlObj.pathname = pathname;

    // Return the path and hash (no origin needed for same-origin navigation)
    return urlObj.pathname + urlObj.hash;
  } catch (error) {
    console.error("Failed to sanitize URL:", error);
    // If parsing fails, return root as safe fallback
    return "/";
  }
}

/**
 * Checks if a URL contains entity-specific IDs
 *
 * @param url The URL to check
 * @returns True if the URL contains entity IDs
 */
export function hasEntityIds(url: string): boolean {
  return ENTITY_ID_PATTERN.test(url);
}

/**
 * Gets the current sanitized URL
 *
 * @returns The current URL without entity IDs or query parameters
 */
export function getCurrentSanitizedUrl(): string {
  return sanitizeUrl(window.location.href);
}
