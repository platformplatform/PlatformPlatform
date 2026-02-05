import { XIcon } from "lucide-react";
import type * as React from "react";
import { createContext, useCallback, useContext, useEffect, useRef, useState } from "react";
import { createPortal } from "react-dom";
import { cn } from "../utils";
import { Button } from "./Button";

// Context for SidePane state
interface SidePaneContextValue {
  isOpen: boolean;
  onClose: () => void;
  needsFullscreen: boolean;
}

const SidePaneContext = createContext<SidePaneContextValue | null>(null);

function useSidePaneContext() {
  const context = useContext(SidePaneContext);
  if (!context) {
    throw new Error("SidePane components must be used within a SidePane.");
  }
  return context;
}

// Hook for accessibility features
function useSidePaneAccessibility(
  isOpen: boolean,
  onClose: () => void,
  needsFullscreen: boolean,
  sidePaneRef: React.RefObject<HTMLElement | null>
) {
  const previouslyFocusedElement = useRef<HTMLElement | null>(null);

  // Store previously focused element
  useEffect(() => {
    if (isOpen && needsFullscreen) {
      previouslyFocusedElement.current = document.activeElement as HTMLElement;
    }
  }, [isOpen, needsFullscreen]);

  // Restore focus on close
  useEffect(() => {
    if (!isOpen && previouslyFocusedElement.current) {
      previouslyFocusedElement.current.focus();
      previouslyFocusedElement.current = null;
    }
  }, [isOpen]);

  // Prevent body scroll when fullscreen
  useEffect(() => {
    if (isOpen && needsFullscreen) {
      const originalStyle = window.getComputedStyle(document.body).overflow;
      document.body.style.overflow = "hidden";
      return () => {
        document.body.style.overflow = originalStyle;
      };
    }
  }, [isOpen, needsFullscreen]);

  // Escape key handler
  useEffect(() => {
    if (!isOpen) {
      return;
    }

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        event.preventDefault();
        onClose();
      }
    };

    document.addEventListener("keydown", handleKeyDown);
    return () => document.removeEventListener("keydown", handleKeyDown);
  }, [isOpen, onClose]);

  // Focus trap for fullscreen mode
  useEffect(() => {
    if (!isOpen || !needsFullscreen || !sidePaneRef.current) {
      return;
    }

    const focusableElements = sidePaneRef.current.querySelectorAll(
      'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
    );
    const firstElement = focusableElements[0] as HTMLElement;
    const lastElement = focusableElements[focusableElements.length - 1] as HTMLElement;

    const handleTabKey = (event: KeyboardEvent) => {
      if (event.key !== "Tab") {
        return;
      }

      if (event.shiftKey && document.activeElement === firstElement) {
        event.preventDefault();
        lastElement.focus();
      } else if (!event.shiftKey && document.activeElement === lastElement) {
        event.preventDefault();
        firstElement.focus();
      }
    };

    document.addEventListener("keydown", handleTabKey);
    return () => document.removeEventListener("keydown", handleTabKey);
  }, [isOpen, needsFullscreen, sidePaneRef]);
}

// Side pane width in rem (matches w-96 = 24rem)
const SIDE_PANE_WIDTH_REM = 24;

// Hook to detect if there's not enough room for side-by-side layout (50-50 split)
// Shows fullscreen if content area can't fit table + side pane at equal widths
function useNeedsFullscreen() {
  const [needsFullscreen, setNeedsFullscreen] = useState(false);

  useEffect(() => {
    const checkSpace = () => {
      const rootFontSize = parseFloat(getComputedStyle(document.documentElement).fontSize);
      const sidePaneWidth = SIDE_PANE_WIDTH_REM * rootFontSize;

      // Get side menu width from CSS variable or use 0 if not present
      const sideMenuWidth =
        parseFloat(getComputedStyle(document.documentElement).getPropertyValue("--side-menu-collapsed-width")) *
          rootFontSize || 0;

      // Available content width (viewport minus side menu)
      const availableWidth = window.innerWidth - sideMenuWidth;

      // Need at least 2x side pane width for 50-50 split
      const minWidthForSideBySide = sidePaneWidth * 2;

      setNeedsFullscreen(availableWidth < minWidthForSideBySide);
    };

    checkSpace();
    window.addEventListener("resize", checkSpace);
    return () => window.removeEventListener("resize", checkSpace);
  }, []);

  return needsFullscreen;
}

// Main SidePane component
interface SidePaneProps {
  children: React.ReactNode;
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
  className?: string;
  "aria-label"?: string;
}

function SidePane({ children, isOpen, onOpenChange, className, "aria-label": ariaLabel }: Readonly<SidePaneProps>) {
  const sidePaneRef = useRef<HTMLElement>(null);
  const needsFullscreen = useNeedsFullscreen();

  const onClose = useCallback(() => {
    onOpenChange(false);
  }, [onOpenChange]);

  useSidePaneAccessibility(isOpen, onClose, needsFullscreen, sidePaneRef);

  if (!isOpen) {
    return null;
  }

  const content = (
    <>
      {/* Backdrop for fullscreen mode */}
      {needsFullscreen && <div className="fixed inset-0 z-[35] bg-black/50" aria-hidden="true" onClick={onClose} />}

      {/* Side pane */}
      <section
        ref={sidePaneRef}
        className={cn(
          "relative flex h-full w-full flex-col border-border border-l bg-card",
          needsFullscreen && "fixed inset-0 z-[45]",
          className
        )}
        aria-label={ariaLabel}
      >
        {children}
      </section>
    </>
  );

  return (
    <SidePaneContext.Provider value={{ isOpen, onClose, needsFullscreen }}>
      {needsFullscreen ? createPortal(content, document.body) : content}
    </SidePaneContext.Provider>
  );
}

// Header component
interface SidePaneHeaderProps {
  children: React.ReactNode;
  className?: string;
  showCloseButton?: boolean;
  closeButtonLabel?: string;
}

function SidePaneHeader({
  children,
  className,
  showCloseButton = true,
  closeButtonLabel = "Close"
}: Readonly<SidePaneHeaderProps>) {
  const { onClose } = useSidePaneContext();

  return (
    <div className={cn("relative flex h-16 shrink-0 items-center border-border border-b px-4", className)}>
      <h4 className="flex h-full items-center">{children}</h4>
      {showCloseButton && (
        <Button
          variant="ghost"
          size="icon-sm"
          onClick={onClose}
          className="absolute top-4 right-4"
          aria-label={closeButtonLabel}
        >
          <XIcon className="size-6" />
        </Button>
      )}
    </div>
  );
}

// Body component with scrolling
interface SidePaneBodyProps {
  children: React.ReactNode;
  className?: string;
}

function SidePaneBody({ children, className }: Readonly<SidePaneBodyProps>) {
  return <div className={cn("flex-1 overflow-y-auto p-4", className)}>{children}</div>;
}

// Footer component
interface SidePaneFooterProps {
  children: React.ReactNode;
  className?: string;
}

function SidePaneFooter({ children, className }: Readonly<SidePaneFooterProps>) {
  return (
    <div className={cn("mt-auto border-border border-t p-4 pb-[max(1rem,env(safe-area-inset-bottom))]", className)}>
      {children}
    </div>
  );
}

// Close button that can be used anywhere
function SidePaneClose({ children, className, ...props }: React.ComponentProps<typeof Button>) {
  const { onClose } = useSidePaneContext();

  return (
    <Button onClick={onClose} className={className} {...props}>
      {children}
    </Button>
  );
}

export { SidePane, SidePaneBody, SidePaneClose, SidePaneFooter, SidePaneHeader };
