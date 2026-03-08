import { toast } from "sonner";

import { applicationInsights } from "../applicationInsights/ApplicationInsightsProvider";
import { showErrorToast } from "./errorHandler";

const DEFAULT_TOAST_DURATIONS = {
  error: 8000
} as const;

function showUnknownErrorToast(error: Error) {
  applicationInsights.trackException({ exception: error });

  toast.error("Unknown Error", {
    description: `An unknown error occurred (${error})`,
    duration: DEFAULT_TOAST_DURATIONS.error
  });
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
      showUnknownErrorToast(event.reason);
    } else {
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

    if (event.error instanceof Error) {
      showUnknownErrorToast(event.error);
    } else {
      const error = new Error(String(event.error));
      showUnknownErrorToast(error);
    }

    return true;
  });
}
