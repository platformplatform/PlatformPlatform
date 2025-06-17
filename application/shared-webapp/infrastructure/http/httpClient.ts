/**
 * Core HTTP utilities for direct, non-strongly-typed fetch requests
 *
 * This module provides fundamental HTTP functionality used both directly and by queryClient.ts:
 * 1. Timeout handling with AbortController
 * 2. Antiforgery token management
 * 3. Consistent error processing across all HTTP requests
 *
 * Use this module when working with endpoints that don't have OpenAPI/strongly-typed definitions
 */
import { normalizeError } from "./errorHandler";

// Default timeout for all fetch requests (in milliseconds)
export const DEFAULT_TIMEOUT = 30000;

/**
 * Gets the antiforgery token from the meta tag
 */
export function getAntiforgeryToken(): string {
  const metaTag = document.querySelector('meta[name="antiforgeryToken"]');
  return metaTag?.getAttribute("content") ?? "";
}

/**
 * Direct fetch wrapper for non-strongly-typed HTTP calls
 * Adds antiforgery tokens, timeout handling, and error processing
 */
export async function enhancedFetch(input: RequestInfo | URL, init?: RequestInit): Promise<Response> {
  const method = init?.method?.toUpperCase() ?? "GET";

  const enhancedInit: RequestInit = { ...init };

  // Add antiforgery token for non-GET requests
  if (method !== "GET") {
    enhancedInit.headers = {
      ...enhancedInit.headers,
      "x-xsrf-token": getAntiforgeryToken()
    };
  }

  // Handle request timeout with AbortController
  const abortController = new AbortController();
  setTimeout(() => {
    abortController.abort(new DOMException("The operation timed out", "TimeoutError"));
  }, DEFAULT_TIMEOUT);

  enhancedInit.signal = abortController.signal;

  try {
    const response = await window.fetch(input, enhancedInit);

    if (!response.ok) {
      throw await normalizeError(response);
    }

    return response;
  } catch (error) {
    throw await normalizeError(error);
  }
}
