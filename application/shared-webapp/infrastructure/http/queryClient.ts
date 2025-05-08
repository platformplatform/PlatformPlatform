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
import { createAuthenticationMiddleware } from "@repo/infrastructure/auth/AuthenticationMiddleware";
import { MutationCache, QueryCache, QueryClient } from "@tanstack/react-query";
import createFetchClient from "openapi-fetch";
import createClient from "openapi-react-query";
import { handleError } from "./errorHandler";
import { DEFAULT_TIMEOUT, getAntiforgeryToken } from "./httpClient";

/**
 * Creates HTTP middleware for the OpenAPI client
 * Handles antiforgery tokens, request timeouts, and error processing
 * Used by the strongly-typed API client to ensure consistent behavior
 */
function createHttpMiddleware() {
  return {
    onRequest: ({ request }: { request: Request }) => {
      // Only add the token for non-GET requests
      if (request.method !== "GET") {
        request.headers.set("x-xsrf-token", getAntiforgeryToken());
      }

      // Handle request timeout with AbortController
      const abortController = new AbortController();
      setTimeout(() => {
        abortController.abort(new DOMException("The operation timed out", "TimeoutError"));
      }, DEFAULT_TIMEOUT);

      // Create a new request with the timeout signal
      const signal = abortController.signal;
      return new Request(request, { signal });
    },

    onResponse: async ({ response }: { request: Request; response: Response }) => {
      if (!response.ok) {
        // Process error directly through handleError to ensure validation errors are properly handled
        const error = await handleError(response);
        return Promise.reject(error);
      }

      return response;
    },

    onRequestError: async ({ error }: { error: unknown; request: Request }) => {
      // Process error directly through handleError to ensure validation errors are properly handled
      const processedError = await handleError(error);
      return Promise.reject(processedError);
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
      retry: false
    }
  },
  queryCache: new QueryCache({
    onError: (error: unknown) => {
      handleError(error);
    }
  }),
  mutationCache: new MutationCache({
    onError: (error: unknown) => {
      handleError(error);
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

  return { api, queryClient };
}
