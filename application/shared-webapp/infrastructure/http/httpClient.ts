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
import { getHasPendingAuthSync } from "../auth/AuthSyncService";
import { normalizeError } from "./errorHandler";

// Default timeout must match GracePeriodSeconds in account/Core/Features/Authentication/Domain/Session.cs
export const DEFAULT_TIMEOUT = 30000;

/**
 * Direct fetch wrapper for non-strongly-typed HTTP calls
 * Adds antiforgery tokens, timeout handling, and error processing
 */
export async function enhancedFetch(input: RequestInfo | URL, init?: RequestInit): Promise<Response> {
  // Don't make API calls if there's a pending auth sync
  if (getHasPendingAuthSync()) {
    const error = new Error("Request blocked due to pending authentication sync");
    error.name = "AbortError";
    throw error;
  }

  const method = init?.method?.toUpperCase() ?? "GET";

  const enhancedInit: RequestInit = { ...init };

  // Add antiforgery token for non-GET requests
  if (method !== "GET") {
    enhancedInit.headers = {
      ...enhancedInit.headers,
      "x-xsrf-token": import.meta.antiforgeryToken
    };
  }

  // Handle request timeout with AbortController
  const abortController = new AbortController();
  setTimeout(() => {
    const error = new Error("The operation timed out");
    error.name = "TimeoutError";
    abortController.abort(error);
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
