/**
 * Centralized HTTP error handling for both TanStack Query and direct fetch operations
 *
 * This module provides consistent error processing across all HTTP requests:
 * 1. Normalizes error handling across different browsers and network conditions
 * 2. Converts HTTP responses and errors into strongly-typed error objects
 * 3. Manages user-facing error notifications with appropriate styling
 * 4. Used by both httpClient.ts and queryClient.ts to ensure consistent error handling
 * 5. Shows toast notifications to the user for unhandled errors
 */

import { toast } from "sonner";
import { applicationInsights } from "../applicationInsights/ApplicationInsightsProvider";

// RFC 7807 Problem Details format
interface ProblemDetails {
  type: string;
  title: string;
  status: number;
  detail?: string;
  errors?: Record<string, string[]>;
  traceId?: string;
}

export interface ServerError extends Error {
  kind: "server";
  status: number;
  problemDetails?: ProblemDetails;
  title?: string | null;
  detail?: string | null;
}

export interface ValidationError extends Error {
  kind: "validation";
  errors: Record<string, string[]>;
  traceId?: string;
  title?: string | null;
  detail?: string | null;
}

export interface TimeoutError extends Error {
  kind: "timeout";
}

export type HttpError = ServerError | ValidationError | TimeoutError;

interface ErrorMessage {
  title: string;
  detail: string;
}

const getServerErrorMessage = (status: number): ErrorMessage => {
  if (status === 502 || status === 503) {
    return {
      title: "Service Unavailable",
      detail: "The service is temporarily unavailable. Please try again later."
    };
  }

  if (status === 504) {
    return {
      title: "Gateway Timeout",
      detail: "The server took too long to respond. Please try again later."
    };
  }

  // Generic 5xx error or unknown errors
  if (status >= 500 || status === 0) {
    return {
      title: "Server Error",
      detail: "The server encountered an error. Please try again later."
    };
  }

  // Generic 4xx error
  return {
    title: "Request Error",
    detail: `The server returned an error (${status})`
  };
};

// Determines toast styling based on HTTP status code
function getToastVariant(status: number): "warning" | "error" {
  // Critical errors that block user flow
  const criticalErrors = [401, 403, 407, 423, 426, 451, ...Array.from({ length: 100 }, (_, i) => i + 500)];
  if (criticalErrors.includes(status)) {
    return "error";
  }

  // All other 4xx errors are warning
  return "warning";
}

const DEFAULT_TOAST_DURATIONS = {
  warning: 6000,
  error: 8000
} as const;

function showTimeoutToast(): void {
  toast.error("Network Error", {
    description: "The server is taking too long to respond. Please try again.",
    duration: DEFAULT_TOAST_DURATIONS.error
  });
}

function showUnknownErrorToast(error: Error) {
  // Track the error in Application Insights
  applicationInsights.trackException({ exception: error });

  toast.error("Unknown Error", {
    description: `An unknown error occurred (${error})`,
    duration: DEFAULT_TOAST_DURATIONS.error
  });
}

function showServerErrorToast(error: ServerError) {
  // Skip showing toast for 401 errors (handled by AuthenticationMiddleware)
  if (error.status === 401) {
    return;
  }

  // Check if this is an anti-forgery error
  if (error.status === 400 && error.problemDetails?.title?.toLowerCase().includes("antiforgery")) {
    // Don't show toast for anti-forgery errors - the auth sync modal will handle it
    // The useAuthSync hook will detect the authentication state mismatch
    return;
  }

  const variant = getToastVariant(error.status);
  const duration = DEFAULT_TOAST_DURATIONS[variant];

  if (error.problemDetails) {
    const { detail, traceId } = error.problemDetails;
    const title = detail ?? "";
    const referenceId = variant === "error" && traceId ? `Reference ID: ${traceId}` : "";

    if (variant === "error") {
      toast.error(title, { description: referenceId, duration });
    } else {
      toast.warning(title, { duration });
    }
  } else {
    const message = getServerErrorMessage(error.status);
    if (variant === "error") {
      toast.error(message.title, { description: message.detail, duration });
    } else {
      toast.warning(message.title, { duration });
    }
  }
}

/**
 * Displays a toast notification for server errors with appropriate styling
 */
export function showErrorToast(error: HttpError): void {
  if (error.kind === "timeout") {
    // Don't show toasts for auth sync blocks - modal will handle it
    if (error.message?.includes("pending authentication")) {
      return;
    }
    showTimeoutToast();
  } else if (error.kind === "server") {
    showServerErrorToast(error);
  } else {
    showUnknownErrorToast(error);
  }
}

function createServerError(status = 0, problemDetails?: ProblemDetails) {
  const serverError = new Error("An unexpected error occurred") as ServerError;
  serverError.kind = "server";
  serverError.status = status;
  serverError.problemDetails = problemDetails;
  return serverError;
}

function createTimeoutError() {
  const timeoutError = new Error("Request timeout") as TimeoutError;
  timeoutError.kind = "timeout";
  return timeoutError;
}

function createValidationError(errors: Record<string, string[]>, traceId?: string) {
  const validationError = new Error("Validation error") as ValidationError;
  validationError.kind = "validation";
  validationError.errors = errors;
  validationError.traceId = traceId;
  return validationError;
}

/**
 * Processes HTTP error responses and converts them to typed error objects
 * Attempts to parse JSON response body and extract ProblemDetails information
 */
async function normalizeHttpResponseError(response: Response): Promise<HttpError> {
  try {
    // Attempt to parse response as JSON
    const data = await response.clone().json();

    // Check if data matches the ProblemDetails structure
    if (typeof data === "object" && data !== null && "title" in data && "status" in data) {
      // Check if it's a validation error
      if (data.errors && Object.keys(data.errors).length > 0) {
        const validationError = createValidationError(data.errors, data.traceId);
        validationError.title = data.title;
        validationError.detail = data.detail ?? null;
        return validationError;
      }

      // Regular server error with ProblemDetails
      const serverError = createServerError(response.status, data);
      serverError.title = data.title;
      serverError.detail = data.detail ?? null;
      return serverError;
    }
  } catch {
    // JSON parsing failed, continue to default error handling
  }

  const serverError = createServerError(response.status);
  const message = getServerErrorMessage(response.status);
  serverError.title = message.title;
  serverError.detail = message.detail;
  return serverError;
}

/**
 * Convert errors during HTTP communication into a normalized HttpError type.
 * Handles HTTP responses, network errors, and already processed errors
 */
export async function normalizeError(errorOrResponse: unknown): Promise<Error | HttpError> {
  // Process HTTP error responses (non-2xx status codes)
  if (errorOrResponse instanceof Response) {
    return await normalizeHttpResponseError(errorOrResponse);
  }

  // Handle network timeout errors and AbortController errors
  if (
    errorOrResponse instanceof Error &&
    (errorOrResponse.name === "TimeoutError" || errorOrResponse.name === "AbortError")
  ) {
    // Don't show timeout errors for auth sync blocks
    if (errorOrResponse.message?.includes("pending authentication sync")) {
      const authBlockedError = new Error("Request blocked due to pending authentication") as TimeoutError;
      authBlockedError.kind = "timeout";
      return authBlockedError;
    }
    return createTimeoutError();
  }

  // Check for Safari-specific timeout errors
  if (
    errorOrResponse instanceof TypeError &&
    (errorOrResponse.message?.includes("The operation couldn't be completed") ||
      errorOrResponse.message?.includes("The network connection was lost"))
  ) {
    return createTimeoutError();
  }

  // Check for "Failed to fetch" errors (works in Chrome/Firefox/Edge)
  if (errorOrResponse instanceof TypeError && errorOrResponse.message?.includes("Failed to fetch")) {
    return createTimeoutError();
  }

  // Return errors that have already been processed (have a 'kind' property)
  if (typeof errorOrResponse === "object" && errorOrResponse !== null && "kind" in errorOrResponse) {
    // These are our custom error types
    return errorOrResponse as unknown as HttpError;
  }

  // If it's already an Error instance, return it directly
  if (errorOrResponse instanceof Error) {
    return errorOrResponse;
  }

  // Handle any other unknown errors
  const serverError = createServerError();
  serverError.title = "An unexpected error occurred";
  serverError.detail = "Please try again later.";
  return serverError;
}

// Track processed errors to prevent showing duplicate error toasts
const processedErrors = new WeakSet<Error | Record<string, unknown>>();

export function setupGlobalErrorHandlers() {
  // Handle uncaught promise rejections
  window.addEventListener("unhandledrejection", (event) => {
    event.preventDefault();

    if (!event.reason) {
      return;
    }
    if (processedErrors.has(event.reason)) {
      return;
    }

    // Don't show errors for auth sync blocks
    if (event.reason instanceof Error && event.reason.message?.includes("pending authentication sync")) {
      return;
    }

    processedErrors.add(event.reason);

    // Check if it's an HttpError or regular Error
    if (event.reason instanceof Error && !("kind" in event.reason)) {
      // Regular JavaScript error - track it and show toast
      showUnknownErrorToast(event.reason);
    } else {
      // HttpError - use existing error handling
      showErrorToast(event.reason);
    }
  });

  // Handle uncaught exceptions
  window.addEventListener("error", (event) => {
    event.preventDefault();

    if (!event.error) {
      return false;
    }
    if (processedErrors.has(event.error)) {
      return false;
    }

    // Don't show errors for auth sync blocks
    if (event.error instanceof Error && event.error.message?.includes("pending authentication sync")) {
      return false;
    }

    processedErrors.add(event.error);

    // Track JavaScript errors in Application Insights and show toast
    if (event.error instanceof Error) {
      showUnknownErrorToast(event.error);
    } else {
      // Create an Error object for non-Error exceptions
      const error = new Error(String(event.error));
      showUnknownErrorToast(error);
    }

    return true; // Stop error propagation
  });
}
