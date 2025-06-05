import type { Response } from "@playwright/test";

/**
 * Check if an HTTP response indicates rate limiting
 * @param response Playwright response object
 * @returns Promise resolving to true if response indicates rate limiting
 */
export async function isRateLimitResponse(response: Response): Promise<boolean> {
  return response.status() === 429;
}
