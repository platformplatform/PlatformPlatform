import { DEFAULT_TOAST_DURATIONS, toastQueue } from "../components/Toast";

type ToastVariant = "info" | "success" | "warning" | "error";

export type ToastOptions = {
  title?: string;
  description?: string;
  variant?: ToastVariant;
  duration?: number;
  action?: React.ReactNode;
};

/**
 * Custom hook for displaying toast notifications
 * @returns Object with toast function to display toast notifications
 */
export function useToast() {
  const toast = (options: ToastOptions) => {
    const { variant = "info", duration, ...rest } = options;

    // Determine the duration - use provided duration or default based on variant
    const toastDuration = duration ?? DEFAULT_TOAST_DURATIONS[variant];

    // Add the toast to the queue
    toastQueue.add(
      {
        ...rest,
        variant,
        duration: toastDuration
      },
      { timeout: toastDuration }
    );
  };

  return { toast };
}
