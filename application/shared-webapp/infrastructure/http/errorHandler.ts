import { toastQueue } from "@repo/ui/components/Toast";

// Type for the problem details format used by ASP.NET Core
export interface ProblemDetails {
  type: string;
  title: string;
  status: number;
  detail?: string;
  errors?: Record<string, string[]>;
  traceId?: string;
}

// Check if the error is a form validation error (has errors property with field validation messages)
const isFormValidationError = (error: unknown): boolean => {
  if (!error || typeof error !== "object") {
    return false;
  }

  const problemDetails = error as ProblemDetails;
  return !!problemDetails.errors && Object.keys(problemDetails.errors).length > 0;
};

type ToastVariant = { variant: "info" | "warning" | "danger"; duration?: number };

// Helper function to determine toast importance based on status code
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

// Helper function to show error toast based on status code
function showErrorToast(status: number, title: string, message: string, traceId?: string): void {
  // Format the message to include the traceId when available
  const formatMessage = (baseMessage: string): string => {
    if (!traceId) {
      return baseMessage;
    }

    // For server errors and unexpected client errors, include the traceId
    // to help support with troubleshooting
    return `${baseMessage}\n\nReference ID: ${traceId}`;
  };

  // Determine toast importance and settings
  const { variant, duration } = getToastVariant(status);

  // Add the toast with appropriate settings
  toastQueue.add({
    variant,
    title: title || "Error", // Use the title from the API response
    description: formatMessage(message),
    duration
  });
}

// Create a middleware for handling API errors
export const createErrorHandlerMiddleware = () => {
  return {
    onResponse: async ({ response }: { response: Response }) => {
      // If the response is not ok (status >= 400), handle the error
      if (!response.ok) {
        const clonedResponse = response.clone();

        try {
          const errorData = (await clonedResponse.json()) as ProblemDetails;

          // Don't show toast for form validation errors as they will be handled by the form
          if (!isFormValidationError(errorData)) {
            const message = errorData.detail || errorData.title || "An error occurred";
            // Show toast based on status code
            showErrorToast(errorData.status, errorData.title, message, errorData.traceId);
          }
        } catch {
          // If we can't parse the response as JSON, show a generic error
          toastQueue.add({
            variant: "danger",
            title: "Error",
            description: "An unexpected error occurred"
          });
        }
      }

      return response;
    },
    onRequestError: ({ error }: { error: Error }) => {
      // Handle network errors or other exceptions
      toastQueue.add({
        variant: "danger",
        title: "Network Error",
        description: "Could not connect to the server"
      });

      throw error; // Re-throw to allow the error to propagate
    }
  };
};
