/**
 * TanStack Query integration with strongly-typed OpenAPI client configuration
 *
 * This module provides a type-safe API layer for interacting with backend services:
 * 1. Configures TanStack Query with consistent caching and error handling
 * 2. Sets up OpenAPI client middleware for strongly-typed API calls
 * 3. Handles antiforgery tokens and timeout for OpenAPI requests
 * 4. Provides centralized API client creation for all applications
 *
 * Use this module when working with endpoints that have OpenAPI/strongly-typed definitions
 */

import { MutationCache, QueryCache, QueryClient } from "@tanstack/react-query";
import createFetchClient from "openapi-fetch";
import createClient from "openapi-react-query";
import { createAuthenticationMiddleware } from "../auth/AuthenticationMiddleware";
import { getHasPendingAuthSync } from "../auth/AuthSyncService";
import { type HttpError, normalizeError } from "./errorHandler";
import { DEFAULT_TIMEOUT } from "./httpClient";

/**
 * Creates HTTP middleware for the OpenAPI client
 * Handles antiforgery tokens, request timeouts, and error processing
 * Used by the strongly-typed API client to ensure consistent behavior
 */
function createHttpMiddleware() {
  return {
    onRequest: ({ request }: { request: Request }) => {
      // Don't make API calls if there's a pending auth sync
      if (getHasPendingAuthSync()) {
        const abortController = new AbortController();
        const error = new Error("Request blocked due to pending authentication sync");
        error.name = "AbortError";
        abortController.abort(error);
        return new Request(request, { signal: abortController.signal });
      }

      // Only add the token for non-GET requests
      if (request.method !== "GET") {
        request.headers.set("x-xsrf-token", import.meta.antiforgeryToken);
      }

      // Handle request timeout with AbortController
      const abortController = new AbortController();
      setTimeout(() => {
        const error = new Error("The operation timed out");
        error.name = "TimeoutError";
        abortController.abort(error);
      }, DEFAULT_TIMEOUT);

      // Create a new request with the timeout signal
      const signal = abortController.signal;
      return new Request(request, { signal });
    },
    onResponse: async ({ response }: { request: Request; response: Response }) => {
      if (!response.ok) {
        // Normalize error and re-throw, so failed requests are handled via error handling
        throw await normalizeError(response);
      }

      return response;
    },
    onRequestError: async ({ error }: { error: unknown; request: Request }) => {
      // Normalize error and re-throw, so failed requests are handled via error handling

      throw await normalizeError(error);
    }
  };
}

/**
 * Singleton QueryClient instance to be used throughout the application
 * This provides consistent caching and error handling for all queries and mutations
 */
export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: false
    },
    mutations: {
      retry: false,
      // Mutations can override this error handler if custom error handling is needed.
      onError: (error: unknown) => {
        // Validation errors in mutations should be handled by UI
        const httpError = error as HttpError;

        // Only skip global error handling for validation errors that have kind: "validation"
        if (httpError?.kind === "validation") {
          return;
        }

        // Re-throwing using "throw" does not bubble the error to the global error handler.
        // We use an unhandled promise rejection instead:
        Promise.reject(error);
      }
    }
  },
  queryCache: new QueryCache({
    onError: (error: unknown) => {
      throw error;
    }
  }),
  mutationCache: new MutationCache({
    onSuccess: (_data, _variables, _context, mutation) => {
      if (mutation.meta?.skipQueryInvalidation) {
        return;
      }
      queryClient.invalidateQueries();
    }
  })
});

/**
 * Creates a typed API client with TanStack Query integration
 * Configures the client with authentication and HTTP middleware
 *
 * @template P - The API paths type from the OpenAPI generated types
 * @returns An object containing the configured API client and TanStack Query hooks
 */
export function createApiClient<P extends Record<string, unknown>>() {
  // Create the fetch client with the consumer's specific API paths type
  const apiClient = createFetchClient<P>({
    baseUrl: import.meta.env.PUBLIC_URL
  });

  // Apply middleware for HTTP handling first, then authentication
  // This ensures errors are processed before authentication redirects
  apiClient.use(createHttpMiddleware());
  apiClient.use(createAuthenticationMiddleware());

  // Create the TanStack Query client
  const api = createClient(apiClient);

  return { api, apiClient, queryClient };
}
