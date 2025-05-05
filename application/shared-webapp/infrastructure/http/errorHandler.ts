/**
 * Centralized HTTP error handling for both TanStack Query and direct fetch operations
 *
 * This module provides consistent error processing across all HTTP requests:
 * 1. Normalizes error handling across different browsers and network conditions
 * 2. Converts HTTP responses and errors into strongly-typed error objects
 * 3. Manages user-facing error notifications with appropriate styling
 * 4. Used by both httpClient.ts and queryClient.ts to ensure consistent error handling
 */
import { toastQueue } from "@repo/ui/components/Toast";

// RFC 7807 Problem Details format
interface ProblemDetails {
  type: string;
  title: string;
  status: number;
  detail?: string;
  errors?: Record<string, string[]>;
  traceId?: string;
}

interface ServerError {
  kind: "server";
  status: number;
  problemDetails?: ProblemDetails;
}

interface ValidationError {
  kind: "validation";
  errors: Record<string, string[]>;
  traceId?: string;
}

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

  // Generic 5xx error
  if (status >= 500) {
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

// Defines toast notification styling and duration options
type ToastVariant = { variant: "info" | "warning" | "danger"; duration?: number };

// Determines toast styling and duration based on HTTP status code
function getToastVariant(status: number): ToastVariant {
  // Success codes are information
  if (status >= 200 && status < 300) {
    return { variant: "info", duration: 3000 }; // 3 seconds for information
  }

  // Critical errors that block user flow
  const criticalErrors = [401, 403, 407, 423, 426, 451, ...Array.from({ length: 100 }, (_, i) => i + 500)];
  if (criticalErrors.includes(status)) {
    return { variant: "danger" }; // No auto-dismiss for critical
  }

  // All other 4xx errors are warning
  return { variant: "warning", duration: 5000 }; // 5 seconds for warning
}

/**
 * Displays a toast notification for network timeout errors
 */
function showTimeoutToast(): void {
  toastQueue.add({
    title: "Network Error",
    description: "The server is taking too long to respond. Please try again.",
    variant: "danger"
  });
}

/**
 * Displays a toast notification for server errors with appropriate styling
 */
function showErrorToast(error: ServerError): void {
  // Skip showing toast for 401 errors (handled by AuthenticationMiddleware)
  if (error.status === 401) {
    return;
  }

  let message: { title: string; detail: string };

  if (error.problemDetails) {
    const { title, detail, traceId } = error.problemDetails;
    message = {
      title,
      detail: traceId ? `${detail ?? ""}\n\nReference ID: ${traceId}` : (detail ?? "")
    };
  } else {
    message = getServerErrorMessage(error.status);
  }

  const toastVariant = getToastVariant(error.status);

  toastQueue.add({
    variant: toastVariant.variant,
    title: message.title,
    description: message.detail ?? "",
    duration: toastVariant.duration
  });
}

/**
 * Processes HTTP error responses and converts them to typed error objects
 * Attempts to parse JSON response body and extract ProblemDetails information
 */
async function handleHttpResponseError(response: Response): Promise<Error> {
  try {
    // Attempt to parse response as JSON
    const data = await response.clone().json();

    // Check if data matches the ProblemDetails structure
    if (typeof data === "object" && data !== null && "type" in data && "title" in data && "status" in data) {
      // Check if it's a validation error
      if (data.errors && Object.keys(data.errors).length > 0) {
        const validationError = new Error("Validation error") as Error & ValidationError;
        validationError.kind = "validation";
        validationError.errors = data.errors;
        validationError.traceId = data.traceId;
        return validationError;
      }

      // Regular server error with ProblemDetails
      const serverError = new Error(data.title ?? "Server error") as Error & ServerError;
      serverError.kind = "server";
      serverError.status = response.status;
      serverError.problemDetails = data;
      showErrorToast(serverError);
      return serverError;
    }
  } catch {
    // JSON parsing failed, continue to default error handling
  }

  const serverError = new Error("Server error") as Error & ServerError;
  serverError.kind = "server";
  serverError.status = response.status;
  showErrorToast(serverError);
  return serverError;
}

/**
 * Central error handler that processes all error types and shows appropriate notifications
 * Handles HTTP responses, network errors, and already processed errors
 */
export async function handleError(errorOrResponse: unknown): Promise<Error> {
  // Process HTTP error responses (non-2xx status codes)
  if (errorOrResponse instanceof Response) {
    return await handleHttpResponseError(errorOrResponse);
  }

  // Handle network timeout errors and AbortController errors
  if (
    errorOrResponse instanceof DOMException &&
    (errorOrResponse.name === "TimeoutError" || errorOrResponse.name === "AbortError")
  ) {
    showTimeoutToast();
    return new Error("Request timeout");
  }

  // Check for Safari-specific timeout errors
  if (
    errorOrResponse instanceof TypeError &&
    (errorOrResponse.message?.includes("The operation couldn't be completed") ||
      errorOrResponse.message?.includes("The network connection was lost"))
  ) {
    showTimeoutToast();
    return new Error("Request timeout");
  }

  // Check for "Failed to fetch" errors (works in Chrome/Firefox/Edge)
  if (errorOrResponse instanceof TypeError && errorOrResponse.message?.includes("Failed to fetch")) {
    showTimeoutToast();
    return new Error("Request timeout");
  }

  // Return errors that have already been processed (have a 'kind' property)
  if (typeof errorOrResponse === "object" && errorOrResponse !== null && "kind" in errorOrResponse) {
    // These are our custom error types (ServerError or ValidationError)
    return errorOrResponse as unknown as Error;
  }

  // If it's already an Error instance, return it directly
  if (errorOrResponse instanceof Error) {
    return errorOrResponse;
  }

  // Handle any other unknown errors
  const serverError = new Error("An unexpected error occurred") as Error & ServerError;
  serverError.kind = "server";
  showErrorToast(serverError);
  return serverError;
}
