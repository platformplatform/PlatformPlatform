import { toast } from "sonner";

type ToastVariant = "info" | "success" | "warning" | "error";

export type ToastOptions = {
  title?: string;
  description?: string;
  variant?: ToastVariant;
  duration?: number;
};

export const DEFAULT_TOAST_DURATIONS = {
  info: 7000,
  success: 7000,
  warning: 11000,
  error: 15000
} as const;

function showToast(options: ToastOptions) {
  const { title, description, variant = "info", duration } = options;
  const toastDuration = duration ?? DEFAULT_TOAST_DURATIONS[variant];

  const toastOptions = {
    description,
    duration: toastDuration
  };

  switch (variant) {
    case "success":
      toast.success(title, toastOptions);
      break;
    case "warning":
      toast.warning(title, toastOptions);
      break;
    case "error":
      toast.error(title, toastOptions);
      break;
    default:
      toast.info(title, toastOptions);
      break;
  }
}

export function useToast() {
  return { toast: showToast };
}
