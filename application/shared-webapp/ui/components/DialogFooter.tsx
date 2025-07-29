import type { ReactNode } from "react";
import { twMerge } from "tailwind-merge";

interface DialogFooterProps {
  children: ReactNode;
  className?: string;
}

interface DialogHeaderProps {
  children: ReactNode;
  className?: string;
  description?: ReactNode;
}

export function DialogFooter({ children, className }: Readonly<DialogFooterProps>) {
  return (
    <div
      className={twMerge(
        "mt-8 flex justify-end gap-4 px-1", // Added px-1 to align with DialogContent padding
        "max-sm:fixed max-sm:right-0 max-sm:bottom-0 max-sm:left-0 max-sm:mt-0 max-sm:border-border max-sm:border-t max-sm:bg-background max-sm:p-4 max-sm:shadow-lg",
        "max-sm:supports-[padding:max(0px)]:pb-[max(1rem,env(safe-area-inset-bottom))]",
        className
      )}
    >
      {children}
    </div>
  );
}

export function DialogHeader({ children, className, description }: Readonly<DialogHeaderProps>) {
  return (
    <div className={twMerge("mb-6", className)}>
      {children}
      {description && <p className="mt-2 text-muted-foreground text-sm">{description}</p>}
    </div>
  );
}

export function DialogContent({ children, className }: Readonly<{ children: ReactNode; className?: string }>) {
  return (
    <div
      className={twMerge(
        "min-h-0 flex-1 overflow-y-auto overflow-x-hidden",
        "p-1", // Add padding to prevent focus ring clipping
        "max-sm:pb-20", // Add padding to account for fixed footer on mobile
        "max-sm:supports-[padding:max(0px)]:pb-[calc(5rem+env(safe-area-inset-bottom))]", // Adjust for safe area
        "-webkit-overflow-scrolling-touch",
        "max-sm:-mx-6 max-sm:px-6", // Extend scrollbar to edge while maintaining content padding
        className
      )}
      style={{
        WebkitOverflowScrolling: "touch",
        scrollbarGutter: "stable"
      }}
    >
      {children}
    </div>
  );
}
