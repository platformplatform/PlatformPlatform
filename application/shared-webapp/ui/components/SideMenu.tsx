import type { Href } from "@react-types/shared";
import { Link as RouterLink, useRouter } from "@tanstack/react-router";
import { ChevronsLeftIcon, type LucideIcon, Menu, X } from "lucide-react";
import type React from "react";
import { createContext, useCallback, useContext, useEffect, useRef, useState } from "react";
import { ToggleButton } from "react-aria-components";
import { tv } from "tailwind-variants";
import { useResponsiveMenu } from "../hooks/useResponsiveMenu";
import logoMarkUrl from "../images/logo-mark.svg";
import { MEDIA_QUERIES, SIDE_MENU_DEFAULT_WIDTH, SIDE_MENU_MAX_WIDTH, SIDE_MENU_MIN_WIDTH } from "../utils/responsive";
import { Button } from "./Button";
import { Link } from "./Link";
import { Tooltip, TooltipTrigger } from "./Tooltip";
import { focusRing } from "./focusRing";

export const collapsedContext = createContext(false);
export const overlayContext = createContext<{ isOpen: boolean; close: () => void } | null>(null);

// Helper function to handle focus trap tab navigation
const _handleFocusTrap = (e: KeyboardEvent, containerRef: React.RefObject<HTMLElement | null>) => {
  if (e.key !== "Tab") {
    return;
  }

  const focusableElements = containerRef.current?.querySelectorAll(
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
};

const menuButtonStyles = tv({
  extend: focusRing,
  base: "menu-item relative flex h-11 w-full items-center justify-start gap-0 overflow-visible rounded-md py-2 pr-2 pl-4 font-normal text-base hover:bg-hover-background focus:outline-none focus-visible:ring-2 focus-visible:ring-ring",
  variants: {
    isCollapsed: {
      true: "",
      false: ""
    },
    isActive: {
      true: "text-foreground",
      false: "text-muted-foreground hover:text-foreground"
    },
    isDisabled: {
      true: "cursor-not-allowed opacity-50 hover:bg-background",
      false: ""
    }
  }
});

const menuTextStyles = tv({
  base: "overflow-hidden whitespace-nowrap text-start",
  variants: {
    isCollapsed: {
      true: "max-w-0 opacity-0",
      false: "max-w-[200px] opacity-100"
    },
    isActive: {
      true: "font-semibold",
      false: "font-normal"
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
  | {
      federatedNavigation: true;
      href: string;
    }
);

// Helper function to get target path from href
const getTargetPath = (to: Href | string, router: ReturnType<typeof useRouter>): string => {
  if (typeof to === "string") {
    return to;
  }
  try {
    return router.buildLocation({ to }).pathname;
  } catch {
    return String(to);
  }
};

// Helper function to normalize path
const normalizePath = (path: string): string => path.replace(/\/$/, "") || "/";

// Helper component for the menu link content
function MenuLinkContent({
  icon: Icon,
  label,
  isActive,
  isCollapsed
}: {
  icon: LucideIcon;
  label: string;
  isActive: boolean;
  isCollapsed: boolean;
}) {
  return (
    <>
      <div className="flex h-6 w-6 shrink-0 items-center justify-center">
        <Icon
          className={`h-5 w-5 ${isActive ? "stroke-foreground" : "stroke-current"} ${isActive && isCollapsed ? "stroke-[2.5px]" : ""}`}
        />
      </div>
      <div className={`${menuTextStyles({ isCollapsed, isActive })} ${isCollapsed ? "" : "ml-4"}`}>{label}</div>
    </>
  );
}

// Helper component for active indicator
function ActiveIndicator({
  isActive,
  isMobileMenu,
  isCollapsed
}: {
  isActive: boolean;
  isMobileMenu: boolean;
  isCollapsed: boolean;
}) {
  if (!isActive) {
    return null;
  }

  return (
    <div
      className={`-translate-y-1/2 absolute top-1/2 h-8 w-1 bg-primary ${
        isMobileMenu ? "-left-3" : isCollapsed ? "-left-2" : "-left-3"
      }`}
    />
  );
}

export function MenuButton({ icon: Icon, label, href: to, isDisabled = false, ...props }: Readonly<MenuButtonProps>) {
  const forceReload = "forceReload" in props ? props.forceReload : false;
  const federatedNavigation = "federatedNavigation" in props ? props.federatedNavigation : false;
  const isCollapsed = useContext(collapsedContext);
  const overlayCtx = useContext(overlayContext);
  const router = useRouter();

  // Check if this menu item is active
  const currentPath = router.state.location.pathname;
  const targetPath = getTargetPath(to, router);
  const isActive = normalizePath(currentPath) === normalizePath(targetPath);

  // Check if we're in the mobile menu context
  const isMobileMenu = !window.matchMedia(MEDIA_QUERIES.sm).matches && !!overlayCtx?.isOpen;

  const linkClassName = menuButtonStyles({ isCollapsed, isActive, isDisabled });

  const handleClick = (e: React.MouseEvent<HTMLAnchorElement>) => {
    if (isDisabled) {
      e.preventDefault();
      return;
    }

    // Auto-close overlay after navigation
    if (overlayCtx?.isOpen) {
      overlayCtx.close();
    }

    // Handle force reload
    if (forceReload) {
      e.preventDefault();
      window.location.href = to;
    }
  };

  const navigateWithDelay = useCallback(() => {
    // Auto-close overlay after navigation
    if (overlayCtx?.isOpen) {
      overlayCtx.close();
    }

    // Smart navigation for federated modules
    if (federatedNavigation) {
      // Check if the target route exists in the current router
      try {
        const matchResult = router.matchRoute({ to });
        if (matchResult !== false) {
          // Route exists in current system - use SPA navigation
          // Don't do anything, let React Aria handle the navigation
          return;
        }
      } catch {
        // Route doesn't exist in current system
      }

      // Route doesn't exist in current system - force reload
      window.location.href = to;
    }
    // Legacy forceReload behavior
    else if (forceReload) {
      window.location.href = to;
    }
  }, [overlayCtx, federatedNavigation, forceReload, router, to]);

  const handlePress = () => {
    if (isDisabled) {
      return;
    }

    // Small delay to ensure touch events are fully processed
    setTimeout(navigateWithDelay, 10);
  };

  // For collapsed menu, wrap in TooltipTrigger
  if (isCollapsed) {
    return (
      <div className="relative">
        <ActiveIndicator isActive={isActive} isMobileMenu={isMobileMenu} isCollapsed={isCollapsed} />
        <TooltipTrigger>
          <Link
            href={forceReload || federatedNavigation ? undefined : to}
            className={linkClassName}
            variant="ghost"
            underline={false}
            isDisabled={isDisabled}
            aria-current={isActive ? "page" : undefined}
            onPress={handlePress}
          >
            <MenuLinkContent icon={Icon} label={label} isActive={isActive} isCollapsed={isCollapsed} />
          </Link>
          <Tooltip placement="right" offset={4}>
            {label}
          </Tooltip>
        </TooltipTrigger>
      </div>
    );
  }

  // For expanded menu
  if (federatedNavigation) {
    // For federated navigation, use React Aria Link to handle smart navigation
    return (
      <div className="relative">
        <ActiveIndicator isActive={isActive} isMobileMenu={isMobileMenu} isCollapsed={isCollapsed} />
        <Link
          href={undefined}
          className={linkClassName}
          variant="ghost"
          underline={false}
          isDisabled={isDisabled}
          aria-current={isActive ? "page" : undefined}
          onPress={handlePress}
        >
          <MenuLinkContent icon={Icon} label={label} isActive={isActive} isCollapsed={isCollapsed} />
        </Link>
      </div>
    );
  }

  // For regular navigation, use TanStack Router Link
  return (
    <div className="relative">
      <ActiveIndicator isActive={isActive} isMobileMenu={isMobileMenu} isCollapsed={isCollapsed} />
      <RouterLink
        to={to}
        className={linkClassName}
        onClick={handleClick}
        disabled={isDisabled}
        aria-current={isActive ? "page" : undefined}
      >
        <MenuLinkContent icon={Icon} label={label} isActive={isActive} isCollapsed={isCollapsed} />
      </RouterLink>
    </div>
  );
}

// Federated menu button for module federation
type FederatedMenuButtonProps = {
  icon: LucideIcon;
  label: string;
  href: string;
  isCurrentSystem: boolean;
  isDisabled?: boolean;
};

export function FederatedMenuButton({
  icon: Icon,
  label,
  href: to,
  isCurrentSystem,
  isDisabled = false
}: Readonly<FederatedMenuButtonProps>) {
  const isCollapsed = useContext(collapsedContext);
  const overlayCtx = useContext(overlayContext);
  const router = useRouter();

  // Check if this menu item is active
  const currentPath = router.state.location.pathname;
  const targetPath = to;
  const isActive = normalizePath(currentPath) === normalizePath(targetPath);

  // Check if we're in the mobile menu context
  const isMobileMenu = !window.matchMedia(MEDIA_QUERIES.sm).matches && !!overlayCtx?.isOpen;

  const linkClassName = menuButtonStyles({ isCollapsed, isActive, isDisabled });

  // For collapsed menu, wrap in TooltipTrigger
  if (isCollapsed) {
    return (
      <div className="relative">
        <ActiveIndicator isActive={isActive} isMobileMenu={isMobileMenu} isCollapsed={isCollapsed} />
        <TooltipTrigger>
          <Link
            href={undefined}
            className={linkClassName}
            variant="ghost"
            underline={false}
            isDisabled={isDisabled}
            aria-current={isActive ? "page" : undefined}
            onPress={() => {
              if (isDisabled) {
                return;
              }

              // Small delay to ensure touch events are fully processed
              setTimeout(() => {
                // Auto-close overlay after navigation
                if (overlayCtx?.isOpen) {
                  overlayCtx.close();
                }

                if (isCurrentSystem) {
                  // Same system - use programmatic navigation
                  window.history.pushState({}, "", to);
                  // Dispatch a popstate event using the standard Event constructor
                  window.dispatchEvent(new Event("popstate"));
                } else {
                  // Different system - force reload
                  window.location.href = to;
                }
              }, 10);
            }}
          >
            <MenuLinkContent icon={Icon} label={label} isActive={isActive} isCollapsed={isCollapsed} />
          </Link>
          <Tooltip placement="right" offset={4}>
            {label}
          </Tooltip>
        </TooltipTrigger>
      </div>
    );
  }

  // For expanded menu, use React Aria Link for consistent touch handling
  return (
    <div className="relative">
      <ActiveIndicator isActive={isActive} isMobileMenu={isMobileMenu} isCollapsed={isCollapsed} />
      <Link
        href={undefined}
        className={linkClassName}
        variant="ghost"
        underline={false}
        isDisabled={isDisabled}
        aria-current={isActive ? "page" : undefined}
        onPress={() => {
          if (isDisabled) {
            return;
          }

          // Small delay to ensure touch events are fully processed
          setTimeout(() => {
            // Auto-close overlay after navigation
            if (overlayCtx?.isOpen) {
              overlayCtx.close();
            }

            if (isCurrentSystem) {
              // Same system - use programmatic navigation
              window.history.pushState({}, "", to);
              // Dispatch a popstate event using the standard Event constructor
              window.dispatchEvent(new Event("popstate"));
            } else {
              // Different system - force reload
              window.location.href = to;
            }
          }, 10);
        }}
      >
        <MenuLinkContent icon={Icon} label={label} isActive={isActive} isCollapsed={isCollapsed} />
      </Link>
    </div>
  );
}

const sideMenuStyles = tv({
  base: "group fixed top-0 left-0 z-50 flex h-screen flex-col bg-background transition-[width] duration-100 md:z-[70]",
  variants: {
    isCollapsed: {
      true: "w-[72px]",
      false: "" // Width will be set inline for resizable menu
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
  },
  compoundVariants: [
    {
      overlayMode: true,
      isOverlayOpen: true,
      class: "w-[300px]" // Match max width constant
    }
  ]
});

const chevronStyles = tv({
  base: "h-4 w-4 transition-transform duration-100",
  variants: {
    isCollapsed: {
      true: "rotate-180 transform",
      false: "rotate-0 transform"
    }
  }
});

type SideMenuProps = {
  children: React.ReactNode;
  sidebarToggleAriaLabel: string;
  mobileMenuAriaLabel: string;
  topMenuContent?: React.ReactNode;
  logoContent?: React.ReactNode;
};

// Helper function to get initial menu width from localStorage
const _getInitialMenuWidth = (): number => {
  const stored = localStorage.getItem("side-menu-size");
  if (stored) {
    const width = Number.parseInt(stored, 10);
    if (!Number.isNaN(width) && width >= SIDE_MENU_MIN_WIDTH && width <= SIDE_MENU_MAX_WIDTH) {
      return width;
    }
  }
  return SIDE_MENU_DEFAULT_WIDTH;
};

// Helper function to get initial collapsed state
const _getInitialCollapsedState = (forceCollapsed: boolean): boolean => {
  if (forceCollapsed) {
    return true;
  }
  return localStorage.getItem("side-menu-collapsed") === "true";
};

// Helper function to get user preference for collapsed state
const _getUserPreference = (): boolean => {
  return localStorage.getItem("side-menu-collapsed") === "true";
};

// Helper function to dispatch menu toggle event
const _dispatchMenuToggleEvent = (overlayMode: boolean, isExpanded: boolean): void => {
  if (overlayMode) {
    window.dispatchEvent(new CustomEvent("side-menu-overlay-toggle", { detail: { isExpanded } }));
  } else {
    window.dispatchEvent(new CustomEvent("side-menu-toggle", { detail: { isCollapsed: !isExpanded } }));
  }
};

// Custom hook for overlay behavior
function useOverlayHandlers({
  overlayMode,
  isOverlayOpen,
  closeOverlay,
  sideMenuRef
}: {
  overlayMode: boolean;
  isOverlayOpen: boolean;
  closeOverlay: () => void;
  sideMenuRef: React.RefObject<HTMLDivElement | null>;
}) {
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
  }, [overlayMode, isOverlayOpen, closeOverlay, sideMenuRef]);

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
      _handleFocusTrap(e, sideMenuRef);
    };

    document.addEventListener("keydown", handleKeyDown);
    return () => document.removeEventListener("keydown", handleKeyDown);
  }, [isOverlayOpen, overlayMode, sideMenuRef]);
}

// Helper to handle resize movement
function createResizeHandler(params: {
  isCollapsed: boolean;
  dragStartPos: React.MutableRefObject<{ x: number; y: number } | null>;
  hasDraggedRef: React.MutableRefObject<boolean>;
  initialMenuWidth: React.MutableRefObject<number>;
  toggleMenu: () => void;
  setIsResizing: (value: boolean) => void;
  setMenuWidth: (value: number) => void;
}) {
  let hasTriggeredCollapse = false;

  const checkDragStarted = (clientX: number, clientY: number) => {
    if (!params.dragStartPos.current) {
      return false;
    }
    const distance =
      Math.abs(clientX - params.dragStartPos.current.x) + Math.abs(clientY - params.dragStartPos.current.y);
    return distance > 5;
  };

  const handleResize = (newWidth: number) => {
    const clampedWidth = Math.min(Math.max(newWidth, SIDE_MENU_MIN_WIDTH), SIDE_MENU_MAX_WIDTH);
    params.setMenuWidth(clampedWidth);
    window.dispatchEvent(new CustomEvent("side-menu-resize", { detail: { width: clampedWidth } }));
  };

  const handleToggle = () => {
    if (!params.isCollapsed) {
      hasTriggeredCollapse = true;
    }
    params.toggleMenu();
    params.setIsResizing(false);
    document.body.style.cursor = "";
  };

  return (e: MouseEvent | TouchEvent) => {
    const { x: clientX, y: clientY } = _getClientCoordinates(e);

    // Check if drag has started
    if (!params.hasDraggedRef.current) {
      params.hasDraggedRef.current = checkDragStarted(clientX, clientY);
    }

    if (!params.hasDraggedRef.current || !params.dragStartPos.current) {
      return;
    }

    const deltaX = clientX - params.dragStartPos.current.x;
    const newWidth = params.initialMenuWidth.current + deltaX;

    // Handle edge cases
    const shouldToggle = params.isCollapsed ? newWidth > 100 : newWidth < 20 && !hasTriggeredCollapse;

    if (shouldToggle) {
      handleToggle();
      return;
    }

    // Normal resize
    if (!params.isCollapsed && !hasTriggeredCollapse) {
      handleResize(newWidth);
    }
  };
}

// Custom hook for resize handling
function useResizeHandler({
  isResizing,
  setIsResizing,
  menuWidth,
  setMenuWidth,
  isCollapsed,
  toggleMenu,
  dragStartPos,
  hasDraggedRef,
  initialMenuWidth
}: {
  isResizing: boolean;
  setIsResizing: (value: boolean) => void;
  menuWidth: number;
  setMenuWidth: (value: number) => void;
  isCollapsed: boolean;
  toggleMenu: () => void;
  dragStartPos: React.MutableRefObject<{ x: number; y: number } | null>;
  hasDraggedRef: React.MutableRefObject<boolean>;
  initialMenuWidth: React.MutableRefObject<number>;
}) {
  useEffect(() => {
    if (!isResizing) {
      return;
    }

    document.body.style.cursor = "col-resize";

    const handleMove = createResizeHandler({
      isCollapsed,
      dragStartPos,
      hasDraggedRef,
      initialMenuWidth,
      toggleMenu,
      setIsResizing,
      setMenuWidth
    });

    const handleEnd = () => {
      setIsResizing(false);
      document.body.style.cursor = "";
      if (!isCollapsed) {
        localStorage.setItem("side-menu-size", menuWidth.toString());
      }
      dragStartPos.current = null;
    };

    document.addEventListener("mousemove", handleMove);
    document.addEventListener("mouseup", handleEnd);
    document.addEventListener("touchmove", handleMove);
    document.addEventListener("touchend", handleEnd);
    document.addEventListener("touchcancel", handleEnd);

    return () => {
      document.removeEventListener("mousemove", handleMove);
      document.removeEventListener("mouseup", handleEnd);
      document.removeEventListener("touchmove", handleMove);
      document.removeEventListener("touchend", handleEnd);
      document.removeEventListener("touchcancel", handleEnd);
      document.body.style.cursor = "";
    };
  }, [
    isResizing,
    menuWidth,
    isCollapsed,
    toggleMenu,
    dragStartPos,
    hasDraggedRef,
    initialMenuWidth,
    setMenuWidth,
    setIsResizing
  ]);
}

// Helper function to save menu preference
const _saveMenuPreference = (isCollapsed: boolean): void => {
  localStorage.setItem("side-menu-collapsed", isCollapsed.toString());
};

// Helper function to focus toggle button
const _focusToggleButton = (toggleButtonRef: React.RefObject<HTMLButtonElement | HTMLDivElement | null>): void => {
  if (!toggleButtonRef.current) {
    return;
  }

  if (toggleButtonRef.current instanceof HTMLButtonElement) {
    toggleButtonRef.current.focus();
  } else {
    const button = toggleButtonRef.current.querySelector("button");
    button?.focus();
  }
};

// Helper function to handle resize with arrow keys
const _handleArrowKeyResize = (
  e: React.KeyboardEvent,
  direction: "left" | "right",
  menuWidth: number,
  setMenuWidth: (width: number) => void
): void => {
  e.preventDefault();
  const delta = direction === "left" ? -10 : 10;
  const newWidth = Math.min(Math.max(menuWidth + delta, SIDE_MENU_MIN_WIDTH), SIDE_MENU_MAX_WIDTH);
  setMenuWidth(newWidth);
  localStorage.setItem("side-menu-size", newWidth.toString());
  window.dispatchEvent(new CustomEvent("side-menu-resize", { detail: { width: newWidth } }));
};

// Helper function to get client coordinates from mouse or touch event
const _getClientCoordinates = (
  e: MouseEvent | TouchEvent | React.MouseEvent | React.TouchEvent
): { x: number; y: number } => {
  if ("touches" in e) {
    return { x: e.touches[0].clientX, y: e.touches[0].clientY };
  }
  return { x: e.clientX, y: e.clientY };
};

// Backdrop component for overlay mode
const OverlayBackdrop = ({ closeOverlay }: { closeOverlay: () => void }) => (
  <div
    className="fixed top-0 right-0 bottom-0 left-[72px] z-40 bg-black/50 transition-opacity duration-100 sm:block md:z-[65] xl:hidden"
    onClick={closeOverlay}
    onKeyDown={(e) => e.key === "Enter" && closeOverlay()}
    role="button"
    tabIndex={0}
    aria-label="Close menu"
  />
);

// Default logo component
const DefaultLogoSection = ({ actualIsCollapsed }: { actualIsCollapsed: boolean }) => (
  <div
    className={actualIsCollapsed ? "flex w-full justify-center" : ""}
    style={
      actualIsCollapsed
        ? undefined
        : {
            display: "grid",
            gridTemplateColumns: "auto 1fr",
            gap: "12px",
            alignItems: "center",
            paddingLeft: "24px",
            paddingRight: "10px",
            width: "100%"
          }
    }
  >
    <img src={logoMarkUrl} alt="Logo" className="h-8 w-8 shrink-0" />
    {!actualIsCollapsed && (
      <span
        className="overflow-hidden text-ellipsis whitespace-nowrap font-semibold text-foreground text-sm"
        style={{ minWidth: 0 }}
      >
        PlatformPlatform
      </span>
    )}
  </div>
);

// Toggle button component for XL screens
const ResizableToggleButton = ({
  toggleButtonRef,
  handleResizeStart,
  hasDraggedRef,
  toggleMenu,
  menuWidth,
  setMenuWidth,
  ariaLabel,
  actualIsCollapsed
}: {
  toggleButtonRef: React.RefObject<HTMLButtonElement>;
  handleResizeStart: (e: React.MouseEvent | React.TouchEvent) => void;
  hasDraggedRef: React.MutableRefObject<boolean>;
  toggleMenu: () => void;
  menuWidth: number;
  setMenuWidth: (width: number) => void;
  ariaLabel: string;
  actualIsCollapsed: boolean;
}) => {
  const handleClick = () => {
    if (!hasDraggedRef.current) {
      toggleMenu();
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    switch (e.key) {
      case "Enter":
      case " ": {
        e.preventDefault();
        toggleMenu();
        break;
      }
      case "ArrowLeft":
        _handleArrowKeyResize(e, "left", menuWidth, setMenuWidth);
        break;
      case "ArrowRight":
        _handleArrowKeyResize(e, "right", menuWidth, setMenuWidth);
        break;
      default:
        break;
    }
  };

  return (
    <button
      ref={toggleButtonRef}
      type="button"
      className="toggle-button flex h-6 w-6 cursor-pointer items-center justify-center rounded-full bg-primary text-primary-foreground opacity-0 transition-opacity duration-100 focus:outline-none focus-visible:opacity-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background group-focus-within:opacity-100 group-hover:opacity-100"
      onMouseDown={handleResizeStart}
      onTouchStart={handleResizeStart}
      onClick={handleClick}
      onKeyDown={handleKeyDown}
      aria-label={ariaLabel}
    >
      <ToggleButtonContent isCollapsed={actualIsCollapsed} />
    </button>
  );
};

// Helper to create toggle button content
const ToggleButtonContent = ({ isCollapsed }: { isCollapsed: boolean }) => (
  <ChevronsLeftIcon className={chevronStyles({ isCollapsed })} />
);

// Menu navigation component
const MenuNav = ({
  sideMenuRef,
  actualIsCollapsed,
  overlayMode,
  isOverlayOpen,
  isHidden,
  className,
  isResizing,
  canResize,
  menuWidth,
  shouldShowResizeHandle,
  handleResizeStart,
  logoContent,
  toggleButtonRef,
  toggleMenu,
  hasDraggedRef,
  setMenuWidth,
  sidebarToggleAriaLabel,
  forceCollapsed,
  children,
  isTenantMenuOpen
}: {
  sideMenuRef: React.RefObject<HTMLDivElement | null>;
  actualIsCollapsed: boolean;
  overlayMode: boolean;
  isOverlayOpen: boolean;
  isHidden: boolean;
  className: string;
  isResizing: boolean;
  canResize: boolean;
  menuWidth: number;
  shouldShowResizeHandle: boolean;
  handleResizeStart: (e: React.MouseEvent | React.TouchEvent) => void;
  logoContent?: React.ReactNode;
  toggleButtonRef: React.RefObject<HTMLButtonElement | HTMLDivElement | null>;
  toggleMenu: () => void;
  hasDraggedRef: React.RefObject<boolean>;
  setMenuWidth: (width: number) => void;
  sidebarToggleAriaLabel?: string;
  forceCollapsed: boolean;
  children: React.ReactNode;
  isTenantMenuOpen: boolean;
}) => (
  <nav
    ref={sideMenuRef}
    className={`${sideMenuStyles({
      isCollapsed: actualIsCollapsed,
      overlayMode,
      isOverlayOpen,
      isHidden: isHidden && !overlayMode,
      className
    })} ${isResizing ? "cursor-col-resize select-none" : ""}`}
    style={canResize ? { width: `${menuWidth}px`, transition: isResizing ? "none" : undefined } : undefined}
    aria-label="Main navigation"
  >
    {/* Vertical divider line - draggable on XL screens */}
    <div
      className={`absolute top-0 right-0 h-full border-border border-r ${
        shouldShowResizeHandle ? "w-2 cursor-col-resize" : ""
      }`}
      onMouseDown={shouldShowResizeHandle ? handleResizeStart : undefined}
      onTouchStart={shouldShowResizeHandle ? handleResizeStart : undefined}
    />

    {/* Fixed header section with logo */}
    <div className="relative flex h-[72px] w-full shrink-0 items-center">
      {logoContent || <DefaultLogoSection actualIsCollapsed={actualIsCollapsed} />}

      {/* Toggle button centered on divider, at intersection with topbar border */}
      <div
        className={`-translate-y-1/2 absolute top-[72px] right-0 translate-x-1/2 ${
          !overlayMode && !forceCollapsed ? "cursor-col-resize" : ""
        } ${isTenantMenuOpen ? "pointer-events-none opacity-0" : ""}`}
      >
        {shouldShowResizeHandle ? (
          <ResizableToggleButton
            toggleButtonRef={toggleButtonRef as React.RefObject<HTMLButtonElement>}
            handleResizeStart={handleResizeStart}
            hasDraggedRef={hasDraggedRef}
            toggleMenu={toggleMenu}
            menuWidth={menuWidth}
            setMenuWidth={setMenuWidth}
            ariaLabel={sidebarToggleAriaLabel || ""}
            actualIsCollapsed={actualIsCollapsed}
          />
        ) : (
          <div ref={toggleButtonRef as React.RefObject<HTMLDivElement>}>
            <ToggleButton
              aria-label={sidebarToggleAriaLabel}
              className="toggle-button flex h-6 w-6 items-center justify-center rounded-full bg-primary text-primary-foreground opacity-0 transition-opacity duration-100 focus:outline-none focus-visible:opacity-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background group-focus-within:opacity-100 group-hover:opacity-100"
              isSelected={actualIsCollapsed}
              onPress={toggleMenu}
            >
              <ToggleButtonContent isCollapsed={actualIsCollapsed} />
            </ToggleButton>
          </div>
        )}
      </div>
    </div>

    {/* Scrollable menu content */}
    <div className={`flex-1 overflow-y-auto ${actualIsCollapsed ? "px-2" : "px-3"} mt-2`}>
      <div className="flex flex-col gap-2 pt-1.5">{children}</div>
    </div>
  </nav>
);

// Custom hook to manage menu state
function useMenuState(forceCollapsed: boolean) {
  const [isOverlayOpen, setIsOverlayOpen] = useState(false);
  const [menuWidth, setMenuWidth] = useState(_getInitialMenuWidth);
  const [isCollapsed, setIsCollapsed] = useState(() => _getInitialCollapsedState(forceCollapsed));
  const [userPreference, setUserPreference] = useState(_getUserPreference);

  // Update collapsed state when screen size changes
  useEffect(() => {
    setIsCollapsed(forceCollapsed || userPreference);
  }, [forceCollapsed, userPreference]);

  return {
    isOverlayOpen,
    setIsOverlayOpen,
    menuWidth,
    setMenuWidth,
    isCollapsed,
    setIsCollapsed,
    userPreference,
    setUserPreference
  };
}

// Custom hook for toggle menu logic
function useToggleMenu({
  overlayMode,
  isOverlayOpen,
  setIsOverlayOpen,
  forceCollapsed,
  isCollapsed,
  setIsCollapsed,
  setUserPreference,
  toggleButtonRef
}: {
  overlayMode: boolean;
  isOverlayOpen: boolean;
  setIsOverlayOpen: (value: boolean) => void;
  forceCollapsed: boolean;
  isCollapsed: boolean;
  setIsCollapsed: (value: boolean) => void;
  setUserPreference: (value: boolean) => void;
  toggleButtonRef: React.RefObject<HTMLButtonElement | HTMLDivElement | null>;
}) {
  const toggleMenu = useCallback(() => {
    if (overlayMode) {
      const newIsOpen = !isOverlayOpen;
      setIsOverlayOpen(newIsOpen);
      _dispatchMenuToggleEvent(true, newIsOpen);
    } else if (!forceCollapsed) {
      const newCollapsed = !isCollapsed;
      setIsCollapsed(newCollapsed);
      setUserPreference(newCollapsed);
      _saveMenuPreference(newCollapsed);
      _dispatchMenuToggleEvent(false, !newCollapsed);
    }
    setTimeout(() => _focusToggleButton(toggleButtonRef), 0);
  }, [
    overlayMode,
    isOverlayOpen,
    forceCollapsed,
    isCollapsed,
    setIsOverlayOpen,
    setIsCollapsed,
    setUserPreference,
    toggleButtonRef
  ]);

  const closeOverlay = useCallback(() => {
    if (!overlayMode || !isOverlayOpen) {
      return;
    }
    setIsOverlayOpen(false);
    _dispatchMenuToggleEvent(true, false);
  }, [overlayMode, isOverlayOpen, setIsOverlayOpen]);

  return { toggleMenu, closeOverlay };
}

export function SideMenu({
  children,
  sidebarToggleAriaLabel,
  mobileMenuAriaLabel,
  topMenuContent,
  logoContent
}: Readonly<SideMenuProps>) {
  const { className, forceCollapsed, overlayMode, isHidden } = useResponsiveMenu();
  const sideMenuRef = useRef<HTMLDivElement>(null);
  const toggleButtonRef = useRef<HTMLButtonElement | HTMLDivElement>(null);
  const [isResizing, setIsResizing] = useState(false);
  const [isTenantMenuOpen, setIsTenantMenuOpen] = useState(false);

  // Use the custom hook for menu state management
  const { isOverlayOpen, setIsOverlayOpen, menuWidth, setMenuWidth, isCollapsed, setIsCollapsed, setUserPreference } =
    useMenuState(forceCollapsed);

  // Listen for tenant menu toggle events
  useEffect(() => {
    const handleTenantMenuToggle = (event: CustomEvent<{ isOpen: boolean }>) => {
      setIsTenantMenuOpen(event.detail.isOpen);
    };

    window.addEventListener("tenant-menu-toggle", handleTenantMenuToggle as EventListener);
    return () => {
      window.removeEventListener("tenant-menu-toggle", handleTenantMenuToggle as EventListener);
    };
  }, []);

  // Compute derived states
  const actualIsCollapsed = overlayMode ? !isOverlayOpen : forceCollapsed || isCollapsed;
  const shouldShowResizeHandle = !overlayMode && !forceCollapsed;
  const canResize = shouldShowResizeHandle && !isCollapsed;

  // Use the toggle menu hook
  const { toggleMenu, closeOverlay } = useToggleMenu({
    overlayMode,
    isOverlayOpen,
    setIsOverlayOpen,
    forceCollapsed,
    isCollapsed,
    setIsCollapsed,
    setUserPreference,
    toggleButtonRef
  });

  // Use custom hooks for overlay behavior
  useOverlayHandlers({
    overlayMode,
    isOverlayOpen,
    closeOverlay,
    sideMenuRef
  });

  // Track mouse movement to detect dragging
  const dragStartPos = useRef<{ x: number; y: number } | null>(null);
  const hasDraggedRef = useRef(false);
  const initialMenuWidth = useRef<number>(0);

  // Handle resize drag for both mouse and touch
  const handleResizeStart = useCallback(
    (e: React.MouseEvent | React.TouchEvent) => {
      e.preventDefault();
      e.stopPropagation();
      setIsResizing(true);

      // Handle both mouse and touch events
      const coords = _getClientCoordinates(e);
      dragStartPos.current = coords;
      hasDraggedRef.current = false;
      initialMenuWidth.current = menuWidth;
    },
    [menuWidth]
  );

  // Extract resize handling into a custom hook to reduce complexity
  useResizeHandler({
    isResizing,
    setIsResizing,
    menuWidth,
    setMenuWidth,
    isCollapsed,
    toggleMenu,
    dragStartPos,
    hasDraggedRef,
    initialMenuWidth
  });

  const menuContent = (
    <collapsedContext.Provider value={actualIsCollapsed}>
      <overlayContext.Provider value={{ isOpen: isOverlayOpen, close: closeOverlay }}>
        <MenuNav
          sideMenuRef={sideMenuRef}
          actualIsCollapsed={actualIsCollapsed}
          overlayMode={overlayMode}
          isOverlayOpen={isOverlayOpen}
          isHidden={isHidden}
          className={className}
          isResizing={isResizing}
          canResize={canResize}
          menuWidth={menuWidth}
          shouldShowResizeHandle={shouldShowResizeHandle}
          handleResizeStart={handleResizeStart}
          logoContent={logoContent}
          toggleButtonRef={toggleButtonRef}
          toggleMenu={toggleMenu}
          hasDraggedRef={hasDraggedRef}
          setMenuWidth={setMenuWidth}
          sidebarToggleAriaLabel={sidebarToggleAriaLabel}
          forceCollapsed={forceCollapsed}
          isTenantMenuOpen={isTenantMenuOpen}
        >
          {children}
        </MenuNav>
      </overlayContext.Provider>
    </collapsedContext.Provider>
  );

  return (
    <>
      {overlayMode && isOverlayOpen && <OverlayBackdrop closeOverlay={closeOverlay} />}
      {menuContent}
      {/* Mobile floating button */}
      <collapsedContext.Provider value={false}>
        <MobileMenu ariaLabel={mobileMenuAriaLabel} topMenuContent={topMenuContent} />
      </collapsedContext.Provider>
    </>
  );
}

const sideMenuSeparatorStyles = tv({
  base: "border-b-0 font-semibold text-muted-foreground uppercase leading-4",
  variants: {
    isCollapsed: {
      true: "-mt-2 mb-2 flex h-8 w-full justify-center",
      false: "h-8 w-full border-border/0 pt-4 pl-4 text-xs"
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
      {isCollapsed ? <div className="w-6 border-border border-b-4" /> : children}
    </div>
  );
}

export function SideMenuSpacer() {
  return <div className="grow" />;
}

// Mobile Menu Component
function MobileMenu({ ariaLabel, topMenuContent }: { ariaLabel: string; topMenuContent?: React.ReactNode }) {
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

  // Listen for custom close event
  useEffect(() => {
    const handleCloseMobileMenu = () => {
      setIsOpen(false);
    };

    window.addEventListener("close-mobile-menu", handleCloseMobileMenu);
    return () => window.removeEventListener("close-mobile-menu", handleCloseMobileMenu);
  }, []);

  // Focus trap and body scroll prevention
  useEffect(() => {
    if (!isOpen) {
      return;
    }

    // Prevent body scroll
    const originalStyle = window.getComputedStyle(document.body).overflow;
    document.body.style.overflow = "hidden";

    const handleKeyDown = (e: KeyboardEvent) => {
      _handleFocusTrap(e, dialogRef);
    };

    document.addEventListener("keydown", handleKeyDown);
    return () => {
      document.removeEventListener("keydown", handleKeyDown);
      // Restore body scroll
      document.body.style.overflow = originalStyle;
    };
  }, [isOpen]);

  return (
    <>
      {!isOpen && (
        <div className="fixed right-4 bottom-4 z-30 supports-[bottom:max(0px)]:bottom-[max(0.75rem,calc(env(safe-area-inset-bottom)-0.25rem))] sm:hidden">
          <Button
            aria-label={ariaLabel}
            className="m-0 inline-flex h-12 w-12 shrink-0 items-center justify-center rounded-full border border-border bg-background pressed:bg-muted p-0 shadow-lg hover:bg-hover-background focus:bg-hover-background"
            onPress={() => setIsOpen(true)}
          >
            <Menu className="h-7 w-7 text-foreground" />
          </Button>
        </div>
      )}
      {isOpen && (
        <overlayContext.Provider value={{ isOpen, close: () => setIsOpen(false) }}>
          <dialog
            className="fixed inset-0 z-[200] h-full w-full bg-background"
            style={{ margin: 0, padding: 0, border: "none", display: "flex" }}
            aria-label="Mobile navigation menu"
            open={true}
            onTouchStart={(e) => e.stopPropagation()}
            onTouchEnd={(e) => e.stopPropagation()}
            onTouchMove={(e) => e.stopPropagation()}
            onClick={(e) => e.stopPropagation()}
            onKeyDown={(e) => {
              if (e.key === "Escape") {
                setIsOpen(false);
              }
            }}
          >
            <nav
              className="flex h-full w-full flex-col bg-background"
              ref={dialogRef}
              style={{ margin: 0, padding: 0 }}
              aria-label="Mobile navigation"
            >
              <div
                className="flex-1 overflow-y-auto overflow-x-hidden px-3 pb-20 supports-[padding:max(0px)]:pb-[max(5rem,env(safe-area-inset-bottom))]"
                style={{
                  margin: 0,
                  paddingLeft: "12px",
                  paddingRight: "12px",
                  pointerEvents: "auto",
                  WebkitOverflowScrolling: "touch",
                  touchAction: "pan-y"
                }}
                onTouchStart={(e) => e.stopPropagation()}
              >
                {topMenuContent && <div className="pt-5">{topMenuContent}</div>}
              </div>

              {/* Floating close button at bottom right - same position as hamburger */}
              <div className="absolute right-4 bottom-4 z-10 supports-[bottom:max(0px)]:bottom-[max(0.75rem,calc(env(safe-area-inset-bottom)-0.25rem))]">
                <Button
                  variant="ghost"
                  size="icon"
                  onPress={() => setIsOpen(false)}
                  aria-label="Close menu"
                  className="h-12 w-12 rounded-full border border-border bg-background/80 shadow-lg backdrop-blur-sm hover:bg-background/90"
                >
                  <X className="h-5 w-5" />
                </Button>
              </div>
            </nav>
          </dialog>
        </overlayContext.Provider>
      )}
    </>
  );
}
