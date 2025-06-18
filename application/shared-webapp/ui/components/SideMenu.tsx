import type { Href } from "@react-types/shared";
import { useRouter } from "@tanstack/react-router";
import { ChevronsLeftIcon, type LucideIcon, X } from "lucide-react";
import type React from "react";
import { createContext, useCallback, useContext, useEffect, useRef, useState } from "react";
import { ToggleButton, composeRenderProps } from "react-aria-components";
import { tv } from "tailwind-variants";
import { useResponsiveMenu } from "../hooks/useResponsiveMenu";
import logoMarkUrl from "../images/logo-mark.svg";
import logoWrapUrl from "../images/logo-wrap.svg";
import { MEDIA_QUERIES } from "../utils/responsive";
import { Button } from "./Button";
import { Dialog, DialogTrigger } from "./Dialog";
import { Modal } from "./Modal";
import { Tooltip, TooltipTrigger } from "./Tooltip";
import { focusRing } from "./focusRing";

const collapsedContext = createContext(false);
const overlayContext = createContext<{ isOpen: boolean; close: () => void } | null>(null);

const menuButtonStyles = tv({
  extend: focusRing,
  base: "menu-item flex h-11 w-full justify-start gap-0 rounded-md py-2 pr-4 pl-4 font-normal text-base text-foreground transition-all duration-100 hover:bg-accent/50 hover:text-foreground/80",
  variants: {
    isCollapsed: {
      true: "ease-out",
      false: "ease-in"
    }
  }
});

const menuTextStyles = tv({
  base: "overflow-hidden whitespace-nowrap text-start text-foreground transition-all duration-100",
  variants: {
    isCollapsed: {
      true: "max-w-0 opacity-0 ease-out",
      false: "max-w-[200px] opacity-100 ease-in"
    }
  }
});

type MenuButtonProps = {
  icon: LucideIcon;
  label: string;
  isDisabled?: boolean;
} & (
  | {
      forceReload?: false;
      href: Href;
    }
  | {
      forceReload: true;
      href: string;
    }
);

export function MenuButton({
  icon: Icon,
  label,
  href: to,
  isDisabled = false,
  forceReload = false
}: Readonly<MenuButtonProps>) {
  const isCollapsed = useContext(collapsedContext);
  const overlayCtx = useContext(overlayContext);
  const { navigate } = useRouter();
  const onPress = () => {
    if (to == null) {
      return;
    }

    // Auto-close overlay after navigation
    if (overlayCtx?.isOpen) {
      overlayCtx.close();
    }

    if (forceReload) {
      window.location.href = to;
    } else {
      navigate({ to });
    }
  };

  return (
    <TooltipTrigger delay={0}>
      <ToggleButton
        className={composeRenderProps("", (className, renderProps) =>
          menuButtonStyles({
            ...renderProps,
            className,
            isCollapsed
          })
        )}
        onPress={onPress}
        isDisabled={isDisabled}
      >
        <div className="flex h-6 w-6 shrink-0 items-center justify-center">
          <Icon className="h-6 w-6 stroke-foreground" />
        </div>
        <div className={`${menuTextStyles({ isCollapsed })} ${isCollapsed ? "" : "ml-4"}`}>{label}</div>
      </ToggleButton>
      {isCollapsed && (
        <Tooltip placement="right" offset={4}>
          {label}
        </Tooltip>
      )}
    </TooltipTrigger>
  );
}

const sideMenuStyles = tv({
  base: "group fixed top-0 left-0 z-50 flex h-screen flex-col bg-background",
  variants: {
    isCollapsed: {
      true: "w-[72px] ease-out",
      false: "w-72 ease-in"
    },
    overlayMode: {
      true: "",
      false: ""
    },
    isOverlayOpen: {
      true: "shadow-2xl",
      false: ""
    },
    isHidden: {
      true: "hidden",
      false: "flex"
    }
  }
});

const chevronStyles = tv({
  base: "h-4 w-4",
  variants: {
    isCollapsed: {
      true: "rotate-180 transform ease-out",
      false: "rotate-0 transform ease-in"
    }
  }
});

type SideMenuProps = {
  children: React.ReactNode;
  ariaLabel: string;
};

export function SideMenu({ children, ariaLabel }: Readonly<SideMenuProps>) {
  const { className, forceCollapsed, overlayMode, isHidden } = useResponsiveMenu();
  const sideMenuRef = useRef<HTMLDivElement>(null);
  const [isOverlayOpen, setIsOverlayOpen] = useState(false);

  // Initialize collapsed state with synchronous check to prevent flicker
  const [isCollapsed, setIsCollapsed] = useState(() => {
    if (typeof window === "undefined") {
      return true;
    }

    // Force collapsed on medium screens
    if (forceCollapsed) {
      return true;
    }

    // Check localStorage for large screens
    try {
      return localStorage.getItem("side-menu-collapsed") === "true";
    } catch {
      return false;
    }
  });

  // Save the user's preference before being forced collapsed
  const [userPreference, setUserPreference] = useState(() => {
    try {
      return localStorage.getItem("side-menu-collapsed") === "true";
    } catch {
      return false;
    }
  });

  // Update collapsed state when screen size changes
  useEffect(() => {
    if (forceCollapsed) {
      // Going to medium screen - force collapse but remember user preference
      setIsCollapsed(true);
    } else {
      // Going back to large screen - restore user preference
      setIsCollapsed(userPreference);
    }
  }, [forceCollapsed, userPreference]);

  // The actual visual collapsed state
  const actualIsCollapsed = overlayMode ? !isOverlayOpen : forceCollapsed || isCollapsed;

  const toggleMenu = () => {
    if (overlayMode) {
      setIsOverlayOpen(!isOverlayOpen);
      // Dispatch event for layout hook
      window.dispatchEvent(
        new CustomEvent("side-menu-overlay-toggle", {
          detail: { isExpanded: !isOverlayOpen }
        })
      );
    } else if (!forceCollapsed) {
      const newCollapsed = !isCollapsed;
      setIsCollapsed(newCollapsed);
      setUserPreference(newCollapsed);
      try {
        localStorage.setItem("side-menu-collapsed", newCollapsed.toString());
      } catch {}
      // Dispatch event for layout hook
      window.dispatchEvent(
        new CustomEvent("side-menu-toggle", {
          detail: { isCollapsed: newCollapsed }
        })
      );
    }
  };

  const closeOverlay = useCallback(() => {
    if (overlayMode && isOverlayOpen) {
      setIsOverlayOpen(false);
      window.dispatchEvent(
        new CustomEvent("side-menu-overlay-toggle", {
          detail: { isExpanded: false }
        })
      );
    }
  }, [overlayMode, isOverlayOpen]);

  // Handle click outside for overlay
  useEffect(() => {
    if (!overlayMode || !isOverlayOpen) {
      return;
    }

    const handleClickOutside = (event: MouseEvent) => {
      if (sideMenuRef.current && !sideMenuRef.current.contains(event.target as Node)) {
        closeOverlay();
      }
    };

    const handleEscape = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        closeOverlay();
      }
    };

    document.addEventListener("mousedown", handleClickOutside);
    document.addEventListener("keydown", handleEscape);

    return () => {
      document.removeEventListener("mousedown", handleClickOutside);
      document.removeEventListener("keydown", handleEscape);
    };
  }, [overlayMode, isOverlayOpen, closeOverlay]);

  // Close mobile menu on resize
  useEffect(() => {
    const handleResize = () => {
      const isNowMedium = window.matchMedia(MEDIA_QUERIES.sm).matches;
      if (isNowMedium && isOverlayOpen) {
        closeOverlay();
      }
    };

    window.addEventListener("resize", handleResize);
    return () => window.removeEventListener("resize", handleResize);
  }, [isOverlayOpen, closeOverlay]);

  // Focus trap for overlay
  useEffect(() => {
    if (!isOverlayOpen || !overlayMode) {
      return;
    }

    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === "Tab") {
        const focusableElements = sideMenuRef.current?.querySelectorAll(
          'button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])'
        );

        if (!focusableElements || focusableElements.length === 0) {
          return;
        }

        const firstElement = focusableElements[0] as HTMLElement;
        const lastElement = focusableElements[focusableElements.length - 1] as HTMLElement;

        if (e.shiftKey && document.activeElement === firstElement) {
          e.preventDefault();
          lastElement.focus();
        } else if (!e.shiftKey && document.activeElement === lastElement) {
          e.preventDefault();
          firstElement.focus();
        }
      }
    };

    document.addEventListener("keydown", handleKeyDown);
    return () => document.removeEventListener("keydown", handleKeyDown);
  }, [isOverlayOpen, overlayMode]);

  return (
    <>
      {/* Backdrop for overlay mode */}
      {overlayMode && isOverlayOpen && (
        <div
          className="fixed top-0 right-0 bottom-0 left-[72px] z-40 bg-black/50 transition-opacity duration-100 sm:block xl:hidden"
          onClick={closeOverlay}
          onKeyDown={(e) => e.key === "Enter" && closeOverlay()}
          role="button"
          tabIndex={0}
          aria-label="Close menu"
        />
      )}

      <collapsedContext.Provider value={actualIsCollapsed}>
        <overlayContext.Provider value={{ isOpen: isOverlayOpen, close: closeOverlay }}>
          <div
            ref={sideMenuRef}
            className={sideMenuStyles({
              isCollapsed: actualIsCollapsed,
              overlayMode,
              isOverlayOpen,
              isHidden: isHidden && !overlayMode,
              className
            })}
          >
            {/* Vertical divider line */}
            <div className="absolute top-0 right-0 h-full border-border/50 border-r opacity-0 transition-opacity duration-100 group-focus-within:opacity-100 group-hover:opacity-100" />

            {/* Fixed header section with logo */}
            <div className="relative flex h-20 w-full shrink-0 items-center">
              {/* Logo container - fixed position */}
              <div className={actualIsCollapsed ? "-mt-5 flex w-full justify-center pt-1" : "-mt-5 pt-1 pl-7"}>
                {actualIsCollapsed ? (
                  <img src={logoMarkUrl} alt="Logo" className="h-8 w-8" />
                ) : (
                  <img src={logoWrapUrl} alt="Logo" className="h-8 w-auto" />
                )}
              </div>

              {/* Toggle button centered on divider, midway between logo and first menu item */}
              <ToggleButton
                aria-label={ariaLabel}
                className={`toggle-button absolute top-[64px] flex h-6 w-6 items-center justify-center rounded-full bg-primary text-primary-foreground opacity-0 transition-all duration-100 focus:outline-none focus-visible:opacity-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background group-focus-within:opacity-100 group-hover:opacity-100 ${
                  actualIsCollapsed ? "-right-3" : "right-0 translate-x-1/2"
                }`}
                isSelected={actualIsCollapsed}
                onPress={toggleMenu}
              >
                <ChevronsLeftIcon className={chevronStyles({ isCollapsed: actualIsCollapsed })} />
              </ToggleButton>
            </div>

            {/* Scrollable menu content */}
            <div className={`flex-1 overflow-y-auto ${actualIsCollapsed ? "px-2" : "px-6"} mt-4`}>
              <div className="flex flex-col gap-2 pt-1">{children}</div>
            </div>
          </div>
        </overlayContext.Provider>
      </collapsedContext.Provider>

      {/* Mobile floating button */}
      <collapsedContext.Provider value={false}>
        <MobileMenu ariaLabel={ariaLabel}>{children}</MobileMenu>
      </collapsedContext.Provider>
    </>
  );
}

const sideMenuSeparatorStyles = tv({
  base: "border-b-0 font-semibold text-muted-foreground uppercase leading-4",
  variants: {
    isCollapsed: {
      true: "-mt-2 mb-2 flex h-8 w-full justify-center",
      false: "h-8 w-full border-border/0 pt-4 pl-4 text-xs ease-in"
    }
  }
});

type SideMenuSeparatorProps = {
  children: React.ReactNode;
};

export function SideMenuSeparator({ children }: Readonly<SideMenuSeparatorProps>) {
  const isCollapsed = useContext(collapsedContext);
  return (
    <div className={sideMenuSeparatorStyles({ isCollapsed })}>
      {isCollapsed ? <div className="w-6 border-border/100 border-b-4" /> : children}
    </div>
  );
}

export function SideMenuSpacer() {
  return <div className="grow" />;
}

// Mobile Menu Component
function MobileMenu({ children, ariaLabel }: { children: React.ReactNode; ariaLabel: string }) {
  const [isOpen, setIsOpen] = useState(false);
  const dialogRef = useRef<HTMLDivElement>(null);

  // Close on resize
  useEffect(() => {
    if (!isOpen) {
      return;
    }

    const handleResize = () => {
      const isNowMedium = window.matchMedia(MEDIA_QUERIES.sm).matches;
      if (isNowMedium) {
        setIsOpen(false);
      }
    };

    window.addEventListener("resize", handleResize);
    return () => window.removeEventListener("resize", handleResize);
  }, [isOpen]);

  // Focus trap
  useEffect(() => {
    if (!isOpen) {
      return;
    }

    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === "Tab") {
        const focusableElements = dialogRef.current?.querySelectorAll(
          'button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])'
        );

        if (!focusableElements || focusableElements.length === 0) {
          return;
        }

        const firstElement = focusableElements[0] as HTMLElement;
        const lastElement = focusableElements[focusableElements.length - 1] as HTMLElement;

        if (e.shiftKey && document.activeElement === firstElement) {
          e.preventDefault();
          lastElement.focus();
        } else if (!e.shiftKey && document.activeElement === lastElement) {
          e.preventDefault();
          firstElement.focus();
        }
      }
    };

    document.addEventListener("keydown", handleKeyDown);
    return () => document.removeEventListener("keydown", handleKeyDown);
  }, [isOpen]);

  return (
    <div className="absolute right-2 bottom-2 z-50 sm:hidden">
      <DialogTrigger>
        <Button
          aria-label={ariaLabel}
          className="inline-flex h-12 w-12 shrink-0 items-center justify-center whitespace-nowrap rounded-md border border-border/50 bg-background/90 text-accent-foreground text-sm shadow-lg ring-offset-background backdrop-blur hover:bg-accent hover:text-accent-foreground/90"
          onPress={() => setIsOpen(true)}
        >
          <img src={logoMarkUrl} alt="Logo" className="h-10 w-10" />
        </Button>
        <Modal
          isDismissable={true}
          isOpen={isOpen}
          onOpenChange={(open) => {
            setIsOpen(open);
            // Dispatch event for layout hook
            window.dispatchEvent(
              new CustomEvent("mobile-menu-toggle", {
                detail: { isOpen: open }
              })
            );
          }}
          blur={false}
          fullSize={true}
        >
          <Dialog className="!h-screen !w-screen !max-h-none !rounded-none !border-0 !bg-background !p-0 !shadow-none dark:!backdrop-blur-0">
            <div className="flex h-screen w-screen flex-col bg-background" ref={dialogRef}>
              <div className="flex h-20 items-center justify-between px-4">
                <img src={logoWrapUrl} alt="Logo" className="h-8 w-auto" />
                <Button variant="ghost" size="icon" onPress={() => setIsOpen(false)} aria-label="Close menu">
                  <X className="h-5 w-5" />
                </Button>
              </div>
              <div className="flex-1 overflow-y-auto px-4">
                <div className="flex flex-col gap-2">{children}</div>
              </div>
            </div>
          </Dialog>
        </Modal>
      </DialogTrigger>
    </div>
  );
}
