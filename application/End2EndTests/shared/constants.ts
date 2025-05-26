/**
 * Shared constants for End2End tests
 */

const DEFAULT_BASE_URL = "https://localhost:9000";

/**
 * Get the base URL for tests
 */
export function getBaseUrl(): string {
  return process.env.PUBLIC_URL ?? DEFAULT_BASE_URL;
}

/**
 * Check if we're running against localhost
 */
export function isLocalhost(): boolean {
  return getBaseUrl() === DEFAULT_BASE_URL;
}
