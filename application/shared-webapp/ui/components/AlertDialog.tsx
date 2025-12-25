import { AlertDialog as AlertDialogPrimitive } from "@base-ui/react/alert-dialog";
import { cva } from "class-variance-authority";
import type * as React from "react";
import { cn } from "../utils";

const overlayStyles = cva(
  "data-[closed]:fade-out-0 data-[open]:fade-in-0 fixed inset-0 isolate flex min-h-dvh w-full items-center justify-center bg-black/15 p-2 text-center duration-200 data-[closed]:animate-out data-[open]:animate-in supports-backdrop-filter:backdrop-blur-xs max-sm:p-0 sm:p-4",
  {
    variants: {
      zIndex: {
        normal: "z-50",
        high: "z-50"
      }
    },
    defaultVariants: {
      zIndex: "high"
    }
  }
);

const popupStyles =
  "data-[closed]:fade-out-0 data-[open]:fade-in-0 data-[closed]:zoom-out-95 data-[open]:zoom-in-95 fixed top-1/2 left-1/2 z-50 flex w-full max-w-[calc(100%-2rem)] -translate-x-1/2 -translate-y-1/2 flex-col overflow-hidden rounded-lg border border-border bg-popover bg-clip-padding p-6 text-left align-middle text-foreground shadow-2xl outline-none duration-200 data-[closed]:animate-out data-[open]:animate-in sm:p-8 dark:backdrop-blur-2xl dark:backdrop-saturate-200 forced-colors:bg-[Canvas]";

type AlertDialogOverlayProps = AlertDialogPrimitive.Backdrop.Props & {
  zIndex?: "normal" | "high";
};

function AlertDialog({ ...props }: AlertDialogPrimitive.Root.Props) {
  return <AlertDialogPrimitive.Root data-slot="alert-dialog" {...props} />;
}

function AlertDialogTrigger({ ...props }: AlertDialogPrimitive.Trigger.Props) {
  return <AlertDialogPrimitive.Trigger data-slot="alert-dialog-trigger" {...props} />;
}

function AlertDialogPortal({ ...props }: AlertDialogPrimitive.Portal.Props) {
  return <AlertDialogPrimitive.Portal data-slot="alert-dialog-portal" {...props} />;
}

function AlertDialogClose({ ...props }: AlertDialogPrimitive.Close.Props) {
  return <AlertDialogPrimitive.Close data-slot="alert-dialog-close" {...props} />;
}

function AlertDialogOverlay({ className, zIndex, ...props }: AlertDialogOverlayProps) {
  return (
    <AlertDialogPrimitive.Backdrop
      data-slot="alert-dialog-overlay"
      className={cn(overlayStyles({ zIndex }), className)}
      {...props}
    />
  );
}

type AlertDialogContentProps = AlertDialogPrimitive.Popup.Props & {
  zIndex?: "normal" | "high";
};

function AlertDialogContent({ className, children, zIndex, ...props }: AlertDialogContentProps) {
  return (
    <AlertDialogPortal>
      <AlertDialogOverlay zIndex={zIndex} />
      <AlertDialogPrimitive.Popup
        data-slot="alert-dialog-content"
        className={cn(popupStyles, "sm:w-dialog-md", className)}
        {...props}
      >
        {children}
      </AlertDialogPrimitive.Popup>
    </AlertDialogPortal>
  );
}

interface AlertDialogSectionProps {
  children: React.ReactNode;
  className?: string;
}

function AlertDialogHeader({ children, className }: Readonly<AlertDialogSectionProps>) {
  return (
    <div data-slot="alert-dialog-header" className={cn("mb-6", className)}>
      {children}
    </div>
  );
}

function AlertDialogFooter({ children, className }: Readonly<AlertDialogSectionProps>) {
  return (
    <div data-slot="alert-dialog-footer" className={cn("flex justify-end gap-4 pt-4", className)}>
      {children}
    </div>
  );
}

function AlertDialogTitle({ className, ...props }: AlertDialogPrimitive.Title.Props) {
  return (
    <AlertDialogPrimitive.Title
      data-slot="alert-dialog-title"
      className={cn("font-semibold text-2xl leading-none", className)}
      {...props}
    />
  );
}

function AlertDialogDescription({ className, ...props }: AlertDialogPrimitive.Description.Props) {
  return (
    <AlertDialogPrimitive.Description
      data-slot="alert-dialog-description"
      className={cn("mt-2 text-muted-foreground text-sm", className)}
      {...props}
    />
  );
}

export {
  AlertDialog,
  AlertDialogClose,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogOverlay,
  AlertDialogPortal,
  AlertDialogTitle,
  AlertDialogTrigger
};
