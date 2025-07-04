import type { Href } from "@react-types/shared";
import { type MakeRouteMatch, useRouter } from "@tanstack/react-router";
import { ChevronsLeftIcon, type LucideIcon, Menu, X } from "lucide-react";
import type React from "react";
import { createContext, useCallback, useContext, useEffect, useRef, useState } from "react";
import { ToggleButton, composeRenderProps } from "react-aria-components";
import { tv } from "tailwind-variants";
import { useResponsiveMenu } from "../hooks/useResponsiveMenu";
import logoMarkUrl from "../images/logo-mark.svg";
import { MEDIA_QUERIES, SIDE_MENU_DEFAULT_WIDTH, SIDE_MENU_MAX_WIDTH, SIDE_MENU_MIN_WIDTH } from "../utils/responsive";
import { Button } from "./Button";
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
  base: "menu-item relative flex h-11 w-full items-center justify-start gap-0 overflow-visible rounded-md py-2 pr-2 pl-4 font-normal text-base hover:bg-hover-background",
  variants: {
    isCollapsed: {
      true: "",
      false: ""
    },
    isActive: {
      true: "text-foreground",
      false: "text-muted-foreground hover:text-foreground"
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
  const router = useRouter();
  const { navigate } = router;

  // Check if this menu item is active
  const currentPath = router.state.location.pathname;
  let targetPath: string;

  if (typeof to === "string") {
    targetPath = to;
  } else {
    try {
      targetPath = router.buildLocation({ to: to as MakeRouteMatch }).pathname;
    } catch {
      // If buildLocation fails, fallback to string representation
      targetPath = String(to);
    }
  }

  // Normalize paths by removing trailing slashes
  const normalizedCurrentPath = currentPath.replace(/\/$/, "") || "/";
  const normalizedTargetPath = targetPath.replace(/\/$/, "") || "/";

  // Check if current path matches the target path exactly
  const isActive = normalizedCurrentPath === normalizedTargetPath;

  const onPress = () => {
    if (to == null) {
      return;
    }

    // Auto-close overlay after navigation (including when clicking active menu item)
    if (overlayCtx?.isOpen) {
      overlayCtx.close();
    }

    // If clicking on the current active page, still close menu but don't navigate
    if (isActive) {
      return;
    }

    if (forceReload) {
      window.location.href = to;
    } else {
      navigate({ to });
    }
  };

  // Check if we're in the mobile menu context
  const isMobileMenu = !window.matchMedia(MEDIA_QUERIES.sm).matches && overlayCtx?.isOpen;

  return (
    <div className="relative">
      {/* Active indicator bar - positioned outside button for proper visibility */}
      {isActive && (
        <div
          className={`-translate-y-1/2 absolute top-1/2 h-8 w-1 bg-primary ${
            isMobileMenu ? "-left-3" : isCollapsed ? "-left-2" : "-left-3"
          }`}
        />
      )}
      <TooltipTrigger delay={0}>
        <ToggleButton
          className={composeRenderProps("", (className, renderProps) =>
            menuButtonStyles({
              ...renderProps,
              className,
              isCollapsed,
              isActive
            })
          )}
          onPress={onPress}
          isDisabled={isDisabled}
        >
          <div className="flex h-6 w-6 shrink-0 items-center justify-center">
            <Icon
              className={`h-5 w-5 ${isActive ? "stroke-foreground" : "stroke-current"} ${isActive && isCollapsed ? "stroke-[2.5px]" : ""}`}
            />
          </div>
          <div className={`${menuTextStyles({ isCollapsed, isActive })} ${isCollapsed ? "" : "ml-4"}`}>{label}</div>
        </ToggleButton>
        {isCollapsed && (
          <Tooltip placement="right" offset={4}>
            {label}
          </Tooltip>
        )}
      </TooltipTrigger>
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
  ariaLabel: string;
  topMenuContent?: React.ReactNode;
  tenantName?: string;
};

export function SideMenu({ children, ariaLabel, topMenuContent, tenantName }: Readonly<SideMenuProps>) {
  const { className, forceCollapsed, overlayMode, isHidden } = useResponsiveMenu();
  const sideMenuRef = useRef<HTMLDivElement>(null);
  const toggleButtonRef = useRef<HTMLButtonElement | HTMLDivElement>(null);
  const [isOverlayOpen, setIsOverlayOpen] = useState(false);
  const [isResizing, setIsResizing] = useState(false);
  const [menuWidth, setMenuWidth] = useState(() => {
    try {
      const stored = localStorage.getItem("side-menu-size");
      if (stored) {
        const width = Number.parseInt(stored, 10);
        if (!Number.isNaN(width) && width >= SIDE_MENU_MIN_WIDTH && width <= SIDE_MENU_MAX_WIDTH) {
          return width;
        }
      }
    } catch {}
    return SIDE_MENU_DEFAULT_WIDTH;
  });

  // Initialize collapsed state with synchronous check to prevent flicker
  const [isCollapsed, setIsCollapsed] = useState(() => {
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

  // Check if we're on XL screen (for resize functionality)
  const isXlScreen = !overlayMode && !forceCollapsed;

  const toggleMenu = useCallback(() => {
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
    // Maintain focus on the toggle button after state change
    setTimeout(() => {
      if (toggleButtonRef.current) {
        if (toggleButtonRef.current instanceof HTMLButtonElement) {
          toggleButtonRef.current.focus();
        } else {
          // For ToggleButton wrapped in div, find the button inside
          const button = toggleButtonRef.current.querySelector("button");
          button?.focus();
        }
      }
    }, 0);
  }, [overlayMode, isOverlayOpen, forceCollapsed, isCollapsed]);

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
      if (dragStartPos.current && !hasDraggedRef.current) {
        const distance = Math.abs(e.clientX - dragStartPos.current.x) + Math.abs(e.clientY - dragStartPos.current.y);
        if (distance > 5) {
          hasDraggedRef.current = true;
        }
      }

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
      if (!isCollapsed && !hasTriggeredCollapse) {
        const newWidth = Math.min(Math.max(mouseX, SIDE_MENU_MIN_WIDTH), SIDE_MENU_MAX_WIDTH);
        setMenuWidth(newWidth);
        // Dispatch event for layout hook during drag
        window.dispatchEvent(
          new CustomEvent("side-menu-resize", {
            detail: { width: newWidth }
          })
        );
      }
    };

    const handleMouseUp = () => {
      setIsResizing(false);
      document.body.style.cursor = "";
      // Only save if we didn't trigger collapse
      if (!hasTriggeredCollapse && !isCollapsed) {
        try {
          localStorage.setItem("side-menu-size", menuWidth.toString());
        } catch {}
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
      {overlayMode && isOverlayOpen && (
        <div
          className="fixed top-0 right-0 bottom-0 left-[72px] z-40 bg-black/50 transition-opacity duration-100 sm:block md:z-[65] xl:hidden"
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
          >
            {/* Vertical divider line - draggable on XL screens */}
            <div
              className={`absolute top-0 right-0 h-full border-border/50 border-r ${
                isXlScreen ? "w-2 cursor-col-resize" : ""
              }`}
              onMouseDown={isXlScreen ? handleResizeStart : undefined}
            />

            {/* Fixed header section with logo */}
            <div className="relative flex h-[72px] w-full shrink-0 items-center">
              {/* Logo and tenant name container */}
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

              {/* Toggle button centered on divider, midway between logo and first menu item */}
              <div
                className={`absolute top-[62px] ${
                  isXlScreen ? "-right-1 translate-x-1/2" : "right-0 translate-x-1/2"
                } ${!overlayMode && !forceCollapsed ? "cursor-col-resize" : ""}`}
              >
                {isXlScreen ? (
                  // Draggable button that acts as resize handle
                  <button
                    ref={toggleButtonRef as React.RefObject<HTMLButtonElement>}
                    type="button"
                    className="toggle-button flex h-6 w-6 cursor-pointer items-center justify-center rounded-full bg-primary text-primary-foreground opacity-0 transition-opacity duration-100 focus:outline-none focus-visible:opacity-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background group-focus-within:opacity-100 group-hover:opacity-100"
                    onMouseDown={handleResizeStart}
                    onClick={(_e) => {
                      // Only toggle if we didn't drag (moved less than 5px)
                      if (!hasDraggedRef.current) {
                        toggleMenu();
                      }
                    }}
                    onKeyDown={(e) => {
                      if (e.key === "Enter" || e.key === " ") {
                        e.preventDefault();
                        toggleMenu();
                      } else if (e.key === "ArrowLeft") {
                        e.preventDefault();
                        const newWidth = Math.max(menuWidth - 10, SIDE_MENU_MIN_WIDTH);
                        setMenuWidth(newWidth);
                        localStorage.setItem("side-menu-size", newWidth.toString());
                        window.dispatchEvent(
                          new CustomEvent("side-menu-resize", {
                            detail: { width: newWidth }
                          })
                        );
                      } else if (e.key === "ArrowRight") {
                        e.preventDefault();
                        const newWidth = Math.min(menuWidth + 10, SIDE_MENU_MAX_WIDTH);
                        setMenuWidth(newWidth);
                        localStorage.setItem("side-menu-size", newWidth.toString());
                        window.dispatchEvent(
                          new CustomEvent("side-menu-resize", {
                            detail: { width: newWidth }
                          })
                        );
                      }
                    }}
                    aria-label={ariaLabel}
                  >
                    <ChevronsLeftIcon className={chevronStyles({ isCollapsed: actualIsCollapsed })} />
                  </button>
                ) : (
                  <div ref={toggleButtonRef as React.RefObject<HTMLDivElement>}>
                    <ToggleButton
                      aria-label={ariaLabel}
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
          </div>
        </overlayContext.Provider>
      </collapsedContext.Provider>

      {/* Mobile floating button */}
      <collapsedContext.Provider value={false}>
        <MobileMenu ariaLabel={ariaLabel} topMenuContent={topMenuContent} />
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
      {isCollapsed ? <div className="w-6 border-border/100 border-b-4" /> : children}
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
        <div className="absolute right-2 bottom-3 z-30 sm:hidden">
          <Button
            aria-label={ariaLabel}
            className="m-0 inline-flex h-12 w-12 shrink-0 items-center justify-center border-0 bg-background pressed:bg-muted p-0 hover:bg-hover-background focus:bg-hover-background"
            onPress={() => setIsOpen(true)}
          >
            <Menu className="h-7 w-7 text-foreground" />
          </Button>
        </div>
      )}
      {isOpen && (
        <overlayContext.Provider value={{ isOpen, close: () => setIsOpen(false) }}>
          <div
            className="fixed inset-0 z-[200] h-[100vh] w-[100vw] bg-background"
            style={{ margin: 0, padding: 0, border: "none", top: 0, left: 0, right: 0, bottom: 0 }}
          >
            <div
              className="flex h-[100vh] w-[100vw] flex-col bg-background"
              ref={dialogRef}
              style={{ margin: 0, padding: 0 }}
            >
              <div
                className="flex-1 overflow-y-auto px-3"
                style={{ margin: 0, padding: "0 12px", pointerEvents: "auto" }}
              >
                {topMenuContent && <div className="pt-5 pb-3">{topMenuContent}</div>}
              </div>

              {/* Floating close button at bottom right - same position as hamburger */}
              <div className="absolute right-2 bottom-3 z-10">
                <Button
                  variant="ghost"
                  size="icon"
                  onPress={() => setIsOpen(false)}
                  aria-label="Close menu"
                  className="h-12 w-12 rounded-full border border-border/50 bg-background/80 shadow-lg backdrop-blur-sm hover:bg-background/90"
                >
                  <X className="h-7 w-7" />
                </Button>
              </div>
            </div>
          </div>
        </overlayContext.Provider>
      )}
    </>
  );
}
