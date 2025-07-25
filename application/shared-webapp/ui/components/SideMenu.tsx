import type { Href } from "@react-types/shared";
import { type MakeRouteMatch, Link as RouterLink, useRouter } from "@tanstack/react-router";
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

const collapsedContext = createContext(false);
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
  base: "menu-item relative flex h-11 w-full items-center justify-start gap-0 overflow-visible rounded-md py-2 pr-2 pl-4 font-normal text-base hover:bg-hover-background focus:outline-none focus-visible:ring-1 focus-visible:ring-ring focus-visible:ring-offset-1",
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
    return router.buildLocation({ to: to as MakeRouteMatch }).pathname;
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

  const handlePress = () => {
    if (isDisabled) {
      return;
    }

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

  const handleClick = (e: React.MouseEvent<HTMLAnchorElement>) => {
    if (isDisabled) {
      e.preventDefault();
      return;
    }

    // Auto-close overlay after navigation
    if (overlayCtx?.isOpen) {
      overlayCtx.close();
    }

    // Always prevent default to handle navigation ourselves
    e.preventDefault();

    if (isCurrentSystem) {
      // Same system - use programmatic navigation
      window.history.pushState({}, "", to);
      // Dispatch a popstate event using the standard Event constructor
      window.dispatchEvent(new Event("popstate"));
    } else {
      // Different system - force reload
      window.location.href = to;
    }
  };

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

  // For expanded menu, use a regular anchor tag with onClick handler
  return (
    <div className="relative">
      <ActiveIndicator isActive={isActive} isMobileMenu={isMobileMenu} isCollapsed={isCollapsed} />
      <a
        href={to}
        className={linkClassName}
        onClick={handleClick}
        aria-disabled={isDisabled}
        aria-current={isActive ? "page" : undefined}
      >
        <MenuLinkContent icon={Icon} label={label} isActive={isActive} isCollapsed={isCollapsed} />
      </a>
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
      class: "w-[320px]" // Wider overlay for longer tenant names
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
  tenantName?: string;
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

// Helper function to check if drag movement has started
const _checkDragStarted = (
  e: MouseEvent,
  dragStartPos: React.MutableRefObject<{ x: number; y: number } | null>,
  hasDraggedRef: React.MutableRefObject<boolean>
): void => {
  if (dragStartPos.current && !hasDraggedRef.current) {
    const distance = Math.abs(e.clientX - dragStartPos.current.x) + Math.abs(e.clientY - dragStartPos.current.y);
    if (distance > 5) {
      hasDraggedRef.current = true;
    }
  }
};

// Helper function to handle resize actions
const _handleResizeAction = (
  mouseX: number,
  isCollapsed: boolean,
  hasTriggeredCollapse: boolean,
  setMenuWidth: (width: number) => void
): void => {
  if (!isCollapsed && !hasTriggeredCollapse) {
    const newWidth = Math.min(Math.max(mouseX, SIDE_MENU_MIN_WIDTH), SIDE_MENU_MAX_WIDTH);
    setMenuWidth(newWidth);
    window.dispatchEvent(new CustomEvent("side-menu-resize", { detail: { width: newWidth } }));
  }
};

// Helper function to dispatch menu toggle event
const _dispatchMenuToggleEvent = (overlayMode: boolean, isExpanded: boolean): void => {
  if (overlayMode) {
    window.dispatchEvent(new CustomEvent("side-menu-overlay-toggle", { detail: { isExpanded } }));
  } else {
    window.dispatchEvent(new CustomEvent("side-menu-toggle", { detail: { isCollapsed: !isExpanded } }));
  }
};

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

// Logo and tenant name component
const LogoSection = ({ actualIsCollapsed, tenantName }: { actualIsCollapsed: boolean; tenantName?: string }) => (
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
        {tenantName || "PlatformPlatform"}
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
  handleResizeStart: (e: React.MouseEvent) => void;
  hasDraggedRef: React.MutableRefObject<boolean>;
  toggleMenu: () => void;
  menuWidth: number;
  setMenuWidth: (width: number) => void;
  ariaLabel: string;
  actualIsCollapsed: boolean;
}) => (
  <button
    ref={toggleButtonRef}
    type="button"
    className="toggle-button flex h-6 w-6 cursor-pointer items-center justify-center rounded-full bg-primary text-primary-foreground opacity-0 transition-opacity duration-100 focus:outline-none focus-visible:opacity-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background group-focus-within:opacity-100 group-hover:opacity-100"
    onMouseDown={handleResizeStart}
    onClick={() => {
      if (!hasDraggedRef.current) {
        toggleMenu();
      }
    }}
    onKeyDown={(e) => {
      if (e.key === "Enter" || e.key === " ") {
        e.preventDefault();
        toggleMenu();
      } else if (e.key === "ArrowLeft") {
        _handleArrowKeyResize(e, "left", menuWidth, setMenuWidth);
      } else if (e.key === "ArrowRight") {
        _handleArrowKeyResize(e, "right", menuWidth, setMenuWidth);
      }
    }}
    aria-label={ariaLabel}
  >
    <ChevronsLeftIcon className={chevronStyles({ isCollapsed: actualIsCollapsed })} />
  </button>
);

export function SideMenu({
  children,
  sidebarToggleAriaLabel,
  mobileMenuAriaLabel,
  topMenuContent,
  tenantName
}: Readonly<SideMenuProps>) {
  const { className, forceCollapsed, overlayMode, isHidden } = useResponsiveMenu();
  const sideMenuRef = useRef<HTMLDivElement>(null);
  const toggleButtonRef = useRef<HTMLButtonElement | HTMLDivElement>(null);
  const [isOverlayOpen, setIsOverlayOpen] = useState(false);
  const [isResizing, setIsResizing] = useState(false);
  const [menuWidth, setMenuWidth] = useState(_getInitialMenuWidth);
  const [isCollapsed, setIsCollapsed] = useState(() => _getInitialCollapsedState(forceCollapsed));
  const [userPreference, setUserPreference] = useState(_getUserPreference);

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

  // Check if we're on XL screen (for resize functionality)
  const isXlScreen = !overlayMode && !forceCollapsed;

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
    // Maintain focus on the toggle button after state change
    setTimeout(() => _focusToggleButton(toggleButtonRef), 0);
  }, [overlayMode, isOverlayOpen, forceCollapsed, isCollapsed]);

  const closeOverlay = useCallback(() => {
    if (overlayMode && isOverlayOpen) {
      setIsOverlayOpen(false);
      _dispatchMenuToggleEvent(true, false);
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
      _handleFocusTrap(e, sideMenuRef);
    };

    document.addEventListener("keydown", handleKeyDown);
    return () => document.removeEventListener("keydown", handleKeyDown);
  }, [isOverlayOpen, overlayMode]);

  // Track mouse movement to detect dragging
  const dragStartPos = useRef<{ x: number; y: number } | null>(null);
  const hasDraggedRef = useRef(false);

  // Handle resize drag
  const handleResizeStart = useCallback((e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setIsResizing(true);
    dragStartPos.current = { x: e.clientX, y: e.clientY };
    hasDraggedRef.current = false;
  }, []);

  useEffect(() => {
    if (!isResizing) {
      return;
    }

    // Add global cursor style
    document.body.style.cursor = "col-resize";

    let hasTriggeredCollapse = false;

    const handleMouseMove = (e: MouseEvent) => {
      const mouseX = e.clientX - 8;

      // Check if mouse has moved more than 5px from start (indicates dragging)
      _checkDragStarted(e, dragStartPos, hasDraggedRef);

      // If dragging from collapsed state and mouse is past collapsed width, expand
      if (isCollapsed && mouseX > 100) {
        toggleMenu();
        setIsResizing(false);
        document.body.style.cursor = "";
        return;
      }

      // If dragging to edge from expanded state, collapse
      if (!isCollapsed && mouseX < 20 && !hasTriggeredCollapse) {
        hasTriggeredCollapse = true;
        toggleMenu();
        setIsResizing(false);
        document.body.style.cursor = "";
        return;
      }

      // Normal resize when expanded
      _handleResizeAction(mouseX, isCollapsed, hasTriggeredCollapse, setMenuWidth);
    };

    const handleMouseUp = () => {
      setIsResizing(false);
      document.body.style.cursor = "";
      // Only save if we didn't trigger collapse
      if (!hasTriggeredCollapse && !isCollapsed) {
        localStorage.setItem("side-menu-size", menuWidth.toString());
      }
      dragStartPos.current = null;
    };

    document.addEventListener("mousemove", handleMouseMove);
    document.addEventListener("mouseup", handleMouseUp);

    return () => {
      document.removeEventListener("mousemove", handleMouseMove);
      document.removeEventListener("mouseup", handleMouseUp);
      document.body.style.cursor = "";
    };
  }, [isResizing, menuWidth, isCollapsed, toggleMenu]);

  return (
    <>
      {/* Backdrop for overlay mode */}
      {overlayMode && isOverlayOpen && <OverlayBackdrop closeOverlay={closeOverlay} />}

      <collapsedContext.Provider value={actualIsCollapsed}>
        <overlayContext.Provider value={{ isOpen: isOverlayOpen, close: closeOverlay }}>
          <nav
            ref={sideMenuRef}
            className={`${sideMenuStyles({
              isCollapsed: actualIsCollapsed,
              overlayMode,
              isOverlayOpen,
              isHidden: isHidden && !overlayMode,
              className
            })} ${isResizing ? "cursor-col-resize select-none" : ""}`}
            style={{
              width: isXlScreen && !isCollapsed ? `${menuWidth}px` : undefined,
              transition: isResizing ? "none" : undefined
            }}
            aria-label="Main navigation"
          >
            {/* Vertical divider line - draggable on XL screens */}
            <div
              className={`absolute top-0 right-0 h-full border-border border-r ${
                isXlScreen ? "w-2 cursor-col-resize" : ""
              }`}
              onMouseDown={isXlScreen ? handleResizeStart : undefined}
            />

            {/* Fixed header section with logo */}
            <div className="relative flex h-[72px] w-full shrink-0 items-center">
              <LogoSection actualIsCollapsed={actualIsCollapsed} tenantName={tenantName} />

              {/* Toggle button centered on divider, at intersection with topbar border */}
              <div
                className={`-translate-y-1/2 absolute top-[72px] right-0 translate-x-1/2 ${
                  !overlayMode && !forceCollapsed ? "cursor-col-resize" : ""
                }`}
              >
                {isXlScreen ? (
                  <ResizableToggleButton
                    toggleButtonRef={toggleButtonRef as React.RefObject<HTMLButtonElement>}
                    handleResizeStart={handleResizeStart}
                    hasDraggedRef={hasDraggedRef}
                    toggleMenu={toggleMenu}
                    menuWidth={menuWidth}
                    setMenuWidth={setMenuWidth}
                    ariaLabel={sidebarToggleAriaLabel}
                    actualIsCollapsed={actualIsCollapsed}
                  />
                ) : (
                  <div ref={toggleButtonRef as React.RefObject<HTMLDivElement>}>
                    <ToggleButton
                      aria-label={sidebarToggleAriaLabel}
                      className={
                        "toggle-button flex h-6 w-6 items-center justify-center rounded-full bg-primary text-primary-foreground opacity-0 transition-opacity duration-100 focus:outline-none focus-visible:opacity-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background group-focus-within:opacity-100 group-hover:opacity-100"
                      }
                      isSelected={actualIsCollapsed}
                      onPress={toggleMenu}
                    >
                      <ChevronsLeftIcon className={chevronStyles({ isCollapsed: actualIsCollapsed })} />
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
        </overlayContext.Provider>
      </collapsedContext.Provider>

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
        <div className="fixed right-2 bottom-3 z-30 sm:hidden">
          <Button
            aria-label={ariaLabel}
            className="m-0 inline-flex h-12 w-12 shrink-0 items-center justify-center border-0 bg-background pressed:bg-muted p-0 shadow-lg hover:bg-hover-background focus:bg-hover-background"
            onPress={() => setIsOpen(true)}
          >
            <Menu className="h-7 w-7 text-foreground" />
          </Button>
        </div>
      )}
      {isOpen && (
        <overlayContext.Provider value={{ isOpen, close: () => setIsOpen(false) }}>
          <dialog
            className="fixed inset-0 z-[200] h-[100vh] w-[100vw] bg-background"
            style={{ margin: 0, padding: 0, border: "none", top: 0, left: 0, right: 0, bottom: 0, display: "flex" }}
            aria-label="Mobile navigation menu"
            open={true}
          >
            <nav
              className="flex h-full w-full flex-col bg-background"
              ref={dialogRef}
              style={{ margin: 0, padding: 0 }}
              aria-label="Mobile navigation"
            >
              <div
                className="flex-1 overflow-y-auto overflow-x-hidden px-3"
                style={{ margin: 0, padding: "0 12px", pointerEvents: "auto", WebkitOverflowScrolling: "touch" }}
              >
                {topMenuContent && <div className="pt-5 pb-20">{topMenuContent}</div>}
              </div>

              {/* Floating close button at bottom right - same position as hamburger */}
              <div className="absolute right-2 bottom-3 z-10">
                <Button
                  variant="ghost"
                  size="icon"
                  onPress={() => setIsOpen(false)}
                  aria-label="Close menu"
                  className="h-12 w-12 rounded-full border border-border bg-background/80 shadow-lg backdrop-blur-sm hover:bg-background/90"
                >
                  <X className="h-7 w-7" />
                </Button>
              </div>
            </nav>
          </dialog>
        </overlayContext.Provider>
      )}
    </>
  );
}
