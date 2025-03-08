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
import { type RefObject, createContext, isValidElement, useContext, useRef } from "react";
import { Button as AriaButton } from "react-aria-components";
import { createPortal } from "react-dom";
import { tv } from "tailwind-variants";
import { focusRing } from "./focusRing";

type ToastVariant = "neutral" | "info" | "success" | "warning" | "danger";

type ToastOptions = {
  variant?: ToastVariant;
  title?: string;
  description?: string;
  action?: React.ReactNode;
};

type ToastContents = React.ReactNode | ToastOptions;

type ToastContext = {
  variant?: ToastVariant;
};

const toastContext = createContext<ToastContext>({});

export const toastQueue = new ToastQueue<ToastContents>({
  maxVisibleToasts: 5
});

export function GlobalToastRegion(props: AriaToastRegionProps) {
  const state = useToastQueue(toastQueue);
  return state.visibleToasts.length > 0 ? createPortal(<ToastRegion {...props} state={state} />, document.body) : null;
}

interface ToastRegionProps<T> extends AriaToastRegionProps {
  state: ToastState<T>;
}

function ToastRegion<T extends ToastContents>({ state, ...props }: Readonly<ToastRegionProps<T>>) {
  const ref = useRef<HTMLDivElement>(null) as RefObject<HTMLDivElement>; // Note(raix): Remove when fixed in react-aria
  const { regionProps } = useToastRegion(props, state, ref);

  return (
    <div
      {...regionProps}
      ref={ref}
      className="fixed top-0 z-[100] flex max-h-screen w-full flex-col-reverse gap-1 p-4 sm:top-auto sm:right-0 sm:bottom-0 sm:flex-col md:max-w-[420px]"
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
      neutral: "bg-popover text-foreground",
      info: "bg-info text-info-foreground",
      success: "bg-success text-success-foreground",
      warning: "bg-warning text-warning-foreground",
      danger: "bg-danger text-danger-foreground"
    }
  },
  defaultVariants: {
    variant: "neutral"
  }
});

const closeButtonStyle = tv({
  extend: focusRing,
  base: [
    "absolute top-2 right-2 rounded-md p-1 text-foreground/50 opacity-0 transition-opacity",
    "hover:text-foreground group-hover:opacity-100"
  ]
});

interface ToastProps<T> extends AriaToastProps<T> {
  state: ToastState<T>;
}

function Toast<T extends ToastContents>({ state, ...props }: Readonly<ToastProps<T>>) {
  const ref = useRef<HTMLDivElement>(null) as RefObject<HTMLDivElement>; // Note(raix): Remove when fixed in react-aria
  const { toastProps, titleProps, closeButtonProps, descriptionProps } = useToast(props, state, ref);
  const { content } = props.toast;

  const closeButton = (
    <AriaButton {...closeButtonProps} className={closeButtonStyle()}>
      <XIcon className="h-4 w-4" />
    </AriaButton>
  );

  // If the content is a ReactNode, render it directly
  if (isReactNode(content)) {
    return (
      <toastContext.Provider value={{ variant: "neutral" }}>
        <div {...toastProps} ref={ref} className={toastStyle()}>
          <div {...titleProps}>{content}</div>
          {closeButton}
        </div>
      </toastContext.Provider>
    );
  }

  const { action, variant, description, title } = content;

  return (
    <toastContext.Provider value={{ variant }}>
      <div {...toastProps} ref={ref} className={toastStyle({ variant })}>
        <div className="grid gap-1">
          {title && (
            <div {...titleProps} className="font-semibold text-sm">
              {title}
            </div>
          )}
          {description && (
            <div {...descriptionProps} className="text-sm opacity-90">
              {description}
            </div>
          )}
        </div>
        <div>{action}</div>
        {closeButton}
      </div>
    </toastContext.Provider>
  );
}

function isReactNode(toast: ToastContents): toast is React.ReactNode {
  return isValidElement(toast);
}

const toastActionStyles = tv({
  extend: focusRing,
  base: [
    "inline-flex h-8 shrink-0 items-center justify-center rounded-md border border-border/50 px-3 font-medium text-sm transition-colors"
  ],
  variants: {
    variant: {
      neutral: "pressed:bg-accent/90 hover:bg-accent hover:text-accent-foreground",
      info: "text-info-foreground hover:border-transparent",
      success: "text-success-foreground hover:border-transparent",
      warning: "text-warning-foreground hover:border-transparent",
      danger: "text-danger-foreground hover:border-transparent"
    },
    isDisabled: {
      true: "pointer-events-none opacity-50"
    }
  },
  defaultVariants: {
    variant: "neutral"
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
