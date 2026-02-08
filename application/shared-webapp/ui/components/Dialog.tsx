import { Dialog as DialogPrimitive } from "@base-ui/react/dialog";
import { XIcon } from "lucide-react";
import type * as React from "react";
import { cloneElement, isValidElement, type ReactNode, useContext } from "react";
import { cn } from "../utils";
import { Button } from "./Button";
import { DirtyDialogContext } from "./DirtyDialog";

export type DialogProps = DialogPrimitive.Root.Props;

function Dialog({ ...props }: DialogProps) {
  return <DialogPrimitive.Root data-slot="dialog" {...props} />;
}

function DialogTrigger({ ...props }: DialogPrimitive.Trigger.Props) {
  return <DialogPrimitive.Trigger data-slot="dialog-trigger" {...props} />;
}

function DialogPortal({ ...props }: DialogPrimitive.Portal.Props) {
  return <DialogPrimitive.Portal data-slot="dialog-portal" {...props} />;
}

// NOTE: This diverges from stock ShadCN to integrate with DirtyDialog.
// When inside a DirtyDialog and the rendered element has type="reset",
// the cancel button bypasses the unsaved changes warning.
function DialogClose({ render, children, ...props }: DialogPrimitive.Close.Props) {
  const dirtyDialogContext = useContext(DirtyDialogContext);
  if (dirtyDialogContext && render && isValidElement(render)) {
    const renderProps = render.props as { type?: string };
    if (renderProps.type === "reset") {
      return cloneElement(render as React.ReactElement<{ onClick?: () => void; children?: ReactNode }>, {
        onClick: dirtyDialogContext.cancel,
        children
      });
    }
  }

  return (
    <DialogPrimitive.Close data-slot="dialog-close" render={render} {...props}>
      {children}
    </DialogPrimitive.Close>
  );
}

function DialogOverlay({ className, ...props }: DialogPrimitive.Backdrop.Props) {
  return (
    <DialogPrimitive.Backdrop
      data-slot="dialog-overlay"
      className={cn(
        "data-closed:fade-out-0 data-open:fade-in-0 fixed inset-0 isolate z-50 bg-black/10 duration-100 data-closed:animate-out data-open:animate-in supports-backdrop-filter:backdrop-blur-xs",
        className
      )}
      {...props}
    />
  );
}

// NOTE: This diverges from stock ShadCN for mobile full-screen dialogs with scrollable content.
// Mobile: full-screen (top-0, h-dvh), Desktop: centered modal (sm:top-1/2, sm:-translate-y-1/2).
function DialogContent({
  className,
  children,
  showCloseButton = true,
  ...props
}: DialogPrimitive.Popup.Props & {
  showCloseButton?: boolean;
}) {
  return (
    <DialogPortal>
      <DialogOverlay />
      <DialogPrimitive.Popup
        data-slot="dialog-content"
        className={cn(
          "data-closed:fade-out-0 data-open:fade-in-0 data-closed:zoom-out-95 data-open:zoom-in-95 fixed left-1/2 z-50 flex w-full -translate-x-1/2 flex-col gap-6 bg-background p-6 text-sm outline-none ring-1 ring-foreground/10 transition-[opacity,transform] duration-100 data-closed:animate-out data-open:animate-in",
          "top-0 h-dvh max-h-dvh max-w-full",
          "sm:top-1/2 sm:h-auto sm:max-h-[calc(100dvh-theme(spacing.16))] sm:-translate-y-1/2 sm:rounded-xl",
          className
        )}
        {...props}
      >
        {children}
        {showCloseButton && (
          <DialogPrimitive.Close
            data-slot="dialog-close"
            render={<Button variant="ghost" className="absolute top-4 right-4" size="icon-sm" />}
          >
            <XIcon className="size-6" />
            <span className="sr-only">Close</span>
          </DialogPrimitive.Close>
        )}
      </DialogPrimitive.Popup>
    </DialogPortal>
  );
}

function DialogHeader({ className, ...props }: React.ComponentProps<"div">) {
  return <div data-slot="dialog-header" className={cn("flex flex-col gap-2", className)} {...props} />;
}

// NOTE: This diverges from stock ShadCN to add padding for focus ring visibility.
// The overflow-y-auto clips focus rings, so p-1 -m-1 provides space for the 3px ring.
function DialogBody({ className, ...props }: React.ComponentProps<"div">) {
  return (
    <div
      data-slot="dialog-body"
      className={cn("-m-1 mb-2 flex min-h-0 flex-1 flex-col gap-4 overflow-y-auto p-1", className)}
      {...props}
    />
  );
}

function DialogFooter({
  className,
  showCloseButton = false,
  children,
  ...props
}: React.ComponentProps<"div"> & {
  showCloseButton?: boolean;
}) {
  return (
    <div
      data-slot="dialog-footer"
      className={cn(
        "mt-auto flex flex-col-reverse gap-2 sm:flex-row sm:justify-end [&>*]:w-full sm:[&>*]:w-auto",
        className
      )}
      {...props}
    >
      {children}
      {showCloseButton && <DialogPrimitive.Close render={<Button variant="outline" />}>Close</DialogPrimitive.Close>}
    </div>
  );
}

// NOTE: This diverges from stock ShadCN to add top margin.
// Removed mt-6 from global h2 styles, so DialogTitle (which is an h2) needs explicit top margin.
function DialogTitle({ className, ...props }: DialogPrimitive.Title.Props) {
  return (
    <DialogPrimitive.Title
      data-slot="dialog-title"
      className={cn("mt-4 font-medium leading-none", className)}
      {...props}
    />
  );
}

function DialogDescription({ className, ...props }: DialogPrimitive.Description.Props) {
  return (
    <DialogPrimitive.Description
      data-slot="dialog-description"
      className={cn(
        "text-muted-foreground text-sm [&>a]:underline [&>a]:underline-offset-3 [&>a]:hover:text-foreground",
        className
      )}
      {...props}
    />
  );
}

export {
  Dialog,
  DialogBody,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogOverlay,
  DialogPortal,
  DialogTitle,
  DialogTrigger
};
