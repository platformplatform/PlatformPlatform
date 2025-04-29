import { toastQueue } from "../components/Toast";

type ToastVariant = "default" | "destructive" | "success" | "warning";

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
    const { variant = "default", duration = 5000, ...rest } = options;

    // Map the variant to the appropriate Toast variant
    const mappedVariant = mapVariant(variant);

    // Add the toast to the queue
    toastQueue.add(
      {
        ...rest,
        variant: mappedVariant
      },
      { timeout: duration }
    );
  };

  return { toast };
}

/**
 * Maps the variant from the public API to the internal Toast component variant
 */
function mapVariant(variant: ToastVariant): "neutral" | "info" | "success" | "warning" | "danger" {
  switch (variant) {
    case "default":
      return "neutral";
    case "destructive":
      return "danger";
    case "success":
      return "success";
    case "warning":
      return "warning";
    default:
      return "neutral";
  }
}
