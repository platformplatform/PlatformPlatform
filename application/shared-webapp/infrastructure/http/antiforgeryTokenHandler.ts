/**
 * HTTP utilities for handling antiforgery tokens in fetch requests
 *
 * This module provides utilities to automatically add antiforgery tokens to HTTP requests
 * for both native fetch and openapi-fetch clients
 */

/**
 * Gets the antiforgery token from the meta tag
 */
export const getAntiforgeryToken = (): string => {
  const metaTag = document.querySelector('meta[name="antiforgeryToken"]');
  return metaTag?.getAttribute("content") ?? "";
};

// Store the original fetch function to avoid recursion
const originalFetch = window.fetch;

/**
 * Fetch function that automatically adds the antiforgery token to non-GET requests
 */
export const fetchWithAntiforgeryToken = (input: RequestInfo | URL, init?: RequestInit): Promise<Response> => {
  const method = init?.method?.toUpperCase() ?? "GET";

  // Create a new init object with the original properties
  const enhancedInit: RequestInit = { ...init };

  // Add antiforgery token for non-GET requests
  if (method !== "GET") {
    enhancedInit.headers = {
      ...enhancedInit.headers,
      "x-xsrf-token": getAntiforgeryToken()
    };
  }

  // Call the original fetch with the enhanced init to avoid recursion
  return originalFetch.call(window, input, enhancedInit);
};

type OpenApiFetchRequestParams = {
  request: Request;
};

/**
 * Initialize HTTP interceptors to add antiforgery tokens to all non-GET requests
 *
 * Call this function once during application startup to ensure all HTTP calls
 * have the necessary antiforgery tokens
 */
export const initializeHttpInterceptors = (): void => {
  window.fetch = (input: RequestInfo | URL, init?: RequestInit): Promise<Response> => {
    return fetchWithAntiforgeryToken(input, init);
  };
};

/**
 * Creates middleware for openapi-fetch clients to add antiforgery tokens
 */
export const createAntiforgeryMiddleware = () => ({
  onRequest: ({ request }: OpenApiFetchRequestParams) => {
    // Only add the token for non-GET requests
    if (request.method !== "GET") {
      request.headers.set("x-xsrf-token", getAntiforgeryToken());
    }
    return request;
  }
});
