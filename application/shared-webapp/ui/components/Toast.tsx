import type { AriaToastProps, AriaToastRegionProps } from "@react-aria/toast";
import { useToast, useToastRegion } from "@react-aria/toast";
import { ToastQueue, type ToastState, useToastQueue } from "@react-stately/toast";
import { XIcon } from "lucide-react";
/**
 * Status: Beta
 * ref: https://react-spectrum.adobe.com/react-spectrum/Toast.html
 * ref: https://ui.shadcn.com/docs/components/toast
 */
import type React from "react";
import { type RefObject, createContext, isValidElement, useContext, useEffect, useRef, useState } from "react";
import { Button as AriaButton } from "react-aria-components";
import { createPortal } from "react-dom";
import { tv } from "tailwind-variants";
import { focusRing } from "./focusRing";

type ToastVariant = "info" | "success" | "warning" | "error";

// Default duration for toasts in milliseconds
export const DEFAULT_TOAST_DURATIONS = {
  info: 4000,
  success: 4000,
  warning: 6000,
  error: 10000
} as const;

type ToastOptions = {
  variant?: ToastVariant;
  title?: string;
  description?: string;
  action?: React.ReactNode;
  duration?: number; // Duration in milliseconds before auto-dismiss, undefined for no auto-dismiss
};

type ToastContents = React.ReactNode | ToastOptions;

type ToastContext = {
  variant?: ToastVariant;
};

const toastContext = createContext<ToastContext>({});

// Create a unique identifier for this instance
const instanceId = Math.random().toString(36).substring(7);

// Create a custom ToastQueue that dispatches events for federation
class FederatedToastQueue<T> extends ToastQueue<T> {
  private isFederatedAdd = false;

  add(content: T, options?: { timeout?: number; priority?: number }) {
    // First add to the local queue
    const result = super.add(content, options);

    // Only dispatch event if this isn't already a federated add
    if (!this.isFederatedAdd) {
      const event = new CustomEvent("federated-toast", {
        detail: { content, options, sourceInstanceId: instanceId },
        bubbles: true,
        composed: true
      });
      window.dispatchEvent(event);
    }

    return result;
  }

  federatedAdd(content: T, options?: { timeout?: number; priority?: number }) {
    this.isFederatedAdd = true;
    const result = this.add(content, options);
    this.isFederatedAdd = false;
    return result;
  }
}

export const toastQueue = new FederatedToastQueue<ToastContents>({
  maxVisibleToasts: 5
});

export function GlobalToastRegion(props: AriaToastRegionProps) {
  const state = useToastQueue(toastQueue);

  // Listen for federated toast events from other modules
  useEffect(() => {
    const handleFederatedToast = (event: CustomEvent) => {
      const { content, options, sourceInstanceId } = event.detail;
      // Only process events from other instances
      if (sourceInstanceId !== instanceId) {
        // Use the federatedAdd method to add without re-dispatching the event
        (toastQueue as FederatedToastQueue<ToastContents>).federatedAdd(content, options);
      }
    };

    window.addEventListener("federated-toast", handleFederatedToast as EventListener);
    return () => {
      window.removeEventListener("federated-toast", handleFederatedToast as EventListener);
    };
  }, []);

  return state.visibleToasts.length > 0 ? createPortal(<ToastRegion {...props} state={state} />, document.body) : null;
}

interface ToastRegionProps<T> extends AriaToastRegionProps {
  state: ToastState<T>;
}

function ToastRegion<T extends ToastContents>({ state, ...props }: Readonly<ToastRegionProps<T>>) {
  const ref = useRef<HTMLDivElement>(null) as RefObject<HTMLDivElement>; // Note(raix): Remove when fixed in react-aria
  const { regionProps } = useToastRegion(props, state, ref);

  // Add keyboard event listener to dismiss toasts with Escape, Enter, or Space keys
  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      // Check if any toasts are visible
      if (state.visibleToasts.length > 0) {
        if (event.key === "Escape") {
          // Dismiss the most recent toast when a dismissal key is pressed
          const mostRecentToast = state.visibleToasts[state.visibleToasts.length - 1];
          if (mostRecentToast) {
            // Use the toastQueue to close the toast
            toastQueue.close(mostRecentToast.key);
            // Prevent default behavior for Space and Enter (page scrolling)
            event.preventDefault();
          }
        }
      }
    };

    document.addEventListener("keydown", handleKeyDown);
    return () => {
      document.removeEventListener("keydown", handleKeyDown);
    };
  }, [state]);

  return (
    <div
      {...regionProps}
      ref={ref}
      className="pointer-events-none fixed top-0 right-0 left-0 z-[100] flex max-h-screen w-full flex-col gap-1 p-2 sm:top-4 sm:right-4 sm:left-auto sm:max-w-[420px] sm:p-0"
    >
      {state.visibleToasts.map((toast) => (
        <Toast key={toast.key} toast={toast} state={state} />
      ))}
    </div>
  );
}

const toastStyle = tv({
  base: "group data-[state=closed]:fade-out-80 data-[state=closed]:slide-out-to-right-full data-[state=open]:slide-in-from-top-full data-[state=open]:sm:slide-in-from-bottom-full pointer-events-auto relative flex w-full items-center justify-between space-x-4 overflow-hidden rounded-md border p-6 pr-8 shadow-lg transition-all data-[swipe=cancel]:translate-x-0 data-[swipe=end]:translate-x-[var(--radix-toast-swipe-end-x)] data-[swipe=move]:translate-x-[var(--radix-toast-swipe-move-x)] data-[state=closed]:animate-out data-[state=open]:animate-in data-[swipe=end]:animate-out data-[swipe=move]:transition-none",
  variants: {
    variant: {
      info: "bg-info text-info-foreground",
      success: "bg-success text-success-foreground",
      warning: "bg-warning text-warning-foreground",
      error: "bg-danger text-danger-foreground"
    }
  },
  defaultVariants: {
    variant: "info"
  }
});

const closeButtonStyle = tv({
  extend: focusRing,
  base: [
    "absolute top-2 right-2 rounded-md p-1 text-white/50 opacity-0 transition-opacity",
    "hover:text-white group-hover:opacity-100"
  ]
});

interface ToastProps<T> extends AriaToastProps<T> {
  state: ToastState<T>;
}

function Toast<T extends ToastContents>({ state, ...props }: Readonly<ToastProps<T>>) {
  const ref = useRef<HTMLFieldSetElement>(null) as RefObject<HTMLFieldSetElement>; // Note(raix): Remove when fixed in react-aria
  const { toastProps, titleProps, closeButtonProps, descriptionProps } = useToast(props, state, ref);
  const { content } = props.toast;

  // Track if the toast is being hovered or focused
  const [isPaused, setIsPaused] = useState(false);
  const timerRef = useRef<NodeJS.Timeout | null>(null);
  const remainingTimeRef = useRef<number | null>(null);
  const startTimeRef = useRef<number | null>(null);

  // Determine the duration for this toast
  const getToastDuration = () => {
    if (content && typeof content === "object" && !isValidElement(content)) {
      const toastOptions = content as ToastOptions;
      // If explicit duration is provided, use it
      if (toastOptions.duration !== undefined) {
        return toastOptions.duration;
      }
      // Otherwise use the default based on variant
      if (toastOptions.variant && toastOptions.variant in DEFAULT_TOAST_DURATIONS) {
        return DEFAULT_TOAST_DURATIONS[toastOptions.variant];
      }
    }
    return undefined;
  };

  const toastDuration = getToastDuration();

  // Handle auto-dismiss functionality with pause on hover/focus
  useEffect(() => {
    // Only set up auto-dismiss if we have a duration
    if (toastDuration !== undefined) {
      // If we're not paused, start/resume the timer
      if (!isPaused) {
        // If we have remaining time from a pause, use that, otherwise use the full duration
        const duration = remainingTimeRef.current !== null ? remainingTimeRef.current : toastDuration;
        startTimeRef.current = Date.now();

        timerRef.current = setTimeout(() => {
          toastQueue.close(props.toast.key);
        }, duration);
      } else if (timerRef.current && startTimeRef.current !== null) {
        // If we're paused and have an active timer, clear it and calculate remaining time
        clearTimeout(timerRef.current);
        timerRef.current = null;

        // Calculate how much time is left
        const elapsedTime = Date.now() - startTimeRef.current;
        remainingTimeRef.current = Math.max(0, toastDuration - elapsedTime);
      }

      // Clean up timer if toast is dismissed manually
      return () => {
        if (timerRef.current) {
          clearTimeout(timerRef.current);
        }
      };
    }
  }, [toastDuration, props.toast.key, isPaused]);

  // Event handlers for mouse and keyboard interactions
  const handleMouseEnter = () => setIsPaused(true);
  const handleMouseLeave = () => setIsPaused(false);
  const handleFocus = () => setIsPaused(true);
  const handleBlur = () => setIsPaused(false);

  const closeButton = (
    <AriaButton {...closeButtonProps} className={closeButtonStyle()}>
      <XIcon className="h-4 w-4" />
    </AriaButton>
  );

  // If the content is a ReactNode, render it directly
  if (isReactNode(content)) {
    return (
      <toastContext.Provider value={{ variant: "info" }}>
        <fieldset
          {...toastProps}
          ref={ref}
          className={`${toastStyle()} border-0 p-6`}
          onMouseEnter={handleMouseEnter}
          onMouseLeave={handleMouseLeave}
          onFocus={handleFocus}
          onBlur={handleBlur}
        >
          <div {...titleProps}>{content}</div>
          {closeButton}
        </fieldset>
      </toastContext.Provider>
    );
  }

  const { action, variant, description, title } = content;

  return (
    <toastContext.Provider value={{ variant }}>
      <fieldset
        {...toastProps}
        ref={ref}
        className={`${toastStyle({ variant })} border-0 p-6`}
        onMouseEnter={handleMouseEnter}
        onMouseLeave={handleMouseLeave}
        onFocus={handleFocus}
        onBlur={handleBlur}
      >
        <div className="grid gap-1">
          {title && (
            <div {...titleProps} className="font-semibold text-sm">
              {title}
            </div>
          )}
          {description && (
            <div {...descriptionProps} className="whitespace-pre-line text-sm opacity-90">
              {description}
            </div>
          )}
        </div>
        <div>{action}</div>
        {closeButton}
      </fieldset>
    </toastContext.Provider>
  );
}

function isReactNode(toast: ToastContents): toast is React.ReactNode {
  return isValidElement(toast);
}

const toastActionStyles = tv({
  extend: focusRing,
  base: [
    "inline-flex h-8 shrink-0 items-center justify-center rounded-md border border-border px-3 font-medium text-sm transition-colors"
  ],
  variants: {
    variant: {
      info: "text-info-foreground hover:border-transparent",
      success: "text-success-foreground hover:border-transparent",
      warning: "text-warning-foreground hover:border-transparent",
      error: "text-danger-foreground hover:border-transparent"
    },
    isDisabled: {
      true: "pointer-events-none opacity-50"
    }
  },
  defaultVariants: {
    variant: "info"
  }
});

type ToastActionProps = {
  altText?: string;
  children: React.ReactNode;
};

export function ToastAction({ children }: Readonly<ToastActionProps>) {
  const { variant } = useContext(toastContext);
  return (
    <AriaButton className={(renderProps) => toastActionStyles({ ...renderProps, variant })}>{children}</AriaButton>
  );
}
