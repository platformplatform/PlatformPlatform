import { t } from "@lingui/core/macro";
import { useRouterState } from "@tanstack/react-router";
import { cva, type VariantProps } from "class-variance-authority";
import { ChevronsLeftIcon, MenuIcon, PanelLeftIcon, XIcon } from "lucide-react";
import * as React from "react";

import { useViewportResize } from "../hooks/useViewportResize";
import { cn } from "../utils";
import {
  getRootFontSize,
  MEDIA_QUERIES,
  SIDE_MENU_COLLAPSED_WIDTH_REM,
  SIDE_MENU_DEFAULT_WIDTH_REM,
  SIDE_MENU_MAX_WIDTH_REM,
  SIDE_MENU_MIN_WIDTH_REM
} from "../utils/responsive";
import { Button } from "./Button";
import { HoverCard, HoverCardContent, HoverCardTrigger } from "./HoverCard";
import { Link } from "./Link";
import { Separator } from "./Separator";
import { Skeleton } from "./Skeleton";
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from "./Tooltip";

// Width is in rem (persisted to localStorage as a rem number).
// Icon-collapsed width comes from responsive.ts so it stays in sync with the old SideMenu migration.
const SIDEBAR_KEYBOARD_SHORTCUT = "b";
const SIDEBAR_STORAGE_KEY_COLLAPSED = "side-menu-collapsed"; // "true" means collapsed
const SIDEBAR_STORAGE_KEY_WIDTH = "side-menu-size"; // rem number
const DRAG_THRESHOLD_PX = 5;

// Minimal `asChild` helper. Clones a single child and merges className and other props.
// Lets callers pass router `<Link>` or other elements where ShadCN uses `asChild`.
type SlotProps = React.HTMLAttributes<HTMLElement> & { children?: React.ReactNode };
function Slot({ children, ...props }: SlotProps) {
  const child = React.Children.only(children);
  if (!React.isValidElement<React.HTMLAttributes<HTMLElement>>(child)) {
    return null;
  }
  const childProps = child.props;
  return React.cloneElement(child, {
    ...childProps,
    ...props,
    className: cn(childProps.className, props.className)
  });
}

type SidebarContextProps = {
  state: "expanded" | "collapsed";
  open: boolean;
  setOpen: (open: boolean) => void;
  openMobile: boolean;
  setOpenMobile: (open: boolean) => void;
  isMobile: boolean;
  toggleSidebar: () => void;
  wrapperRef: React.RefObject<HTMLDivElement | null>;
  setWidthRem: (rem: number) => void;
};

const SidebarContext = React.createContext<SidebarContextProps | null>(null);

// Legacy shims matching the old SideMenu.tsx context surface.
// Federated modules (UserMenu, MobileMenu, MobileMenuContent) still import these names.
// Provided by SidebarProvider with values derived from useSidebar state — no consumer changes needed.
const collapsedContext = React.createContext(false);
const overlayContext = React.createContext<{ isOpen: boolean; close: () => void } | null>(null);

function useSidebar() {
  const context = React.useContext(SidebarContext);
  if (!context) {
    throw new Error("useSidebar must be used within a SidebarProvider.");
  }
  return context;
}

// Safe variant for components that may render with or without a SidebarProvider ancestor
// (e.g. AppLayout, which still supports the legacy SideMenu until all SCSs are migrated).
function useSidebarSafe() {
  return React.useContext(SidebarContext);
}

function readInitialOpen(defaultOpen: boolean) {
  if (typeof window === "undefined") {
    return defaultOpen;
  }
  const stored = localStorage.getItem(SIDEBAR_STORAGE_KEY_COLLAPSED);
  if (stored === null) {
    return defaultOpen;
  }
  return stored !== "true";
}

function readInitialWidthRem() {
  if (typeof window === "undefined") {
    return SIDE_MENU_DEFAULT_WIDTH_REM;
  }
  const stored = localStorage.getItem(SIDEBAR_STORAGE_KEY_WIDTH);
  if (stored === null) {
    return SIDE_MENU_DEFAULT_WIDTH_REM;
  }
  const parsed = Number.parseFloat(stored);
  if (Number.isNaN(parsed)) {
    return SIDE_MENU_DEFAULT_WIDTH_REM;
  }
  return Math.min(Math.max(parsed, SIDE_MENU_MIN_WIDTH_REM), SIDE_MENU_MAX_WIDTH_REM);
}

function SidebarProvider({
  defaultOpen = true,
  open: openProp,
  onOpenChange: setOpenProp,
  className,
  style,
  children,
  ...props
}: React.ComponentProps<"div"> & {
  defaultOpen?: boolean;
  open?: boolean;
  onOpenChange?: (open: boolean) => void;
}) {
  const isMobile = useViewportResize();
  const [openMobile, setOpenMobile] = React.useState(false);
  const wrapperRef = React.useRef<HTMLDivElement>(null);

  // Persisted user preference (only mutated by explicit toggles at `lg+`). This is the value
  // the sidebar restores to when the viewport grows back from overlay mode.
  const persistedOpenRef = React.useRef<boolean>(readInitialOpen(defaultOpen));
  const [internalOpen, setInternalOpen] = React.useState<boolean>(() => {
    if (typeof window === "undefined") {
      return persistedOpenRef.current;
    }
    // On initial load below `xl`, force-collapsed regardless of stored preference.
    return window.matchMedia(MEDIA_QUERIES.xl).matches ? persistedOpenRef.current : false;
  });
  const open = openProp ?? internalOpen;
  const setOpen = React.useCallback(
    (value: boolean | ((prev: boolean) => boolean)) => {
      const openState = typeof value === "function" ? value(open) : value;
      if (setOpenProp) {
        setOpenProp(openState);
      } else {
        setInternalOpen(openState);
      }
      // Only persist user choices made at `xl+`. Toggles below `xl` are overlay-mode
      // interactions — transient, never written to localStorage.
      if (typeof window !== "undefined" && window.matchMedia(MEDIA_QUERIES.xl).matches) {
        persistedOpenRef.current = openState;
        localStorage.setItem(SIDEBAR_STORAGE_KEY_COLLAPSED, String(!openState));
      }
    },
    [setOpenProp, open]
  );

  const toggleSidebar = React.useCallback(() => {
    if (isMobile) {
      setOpenMobile((prev) => !prev);
    } else {
      setOpen((prev) => !prev);
    }
  }, [isMobile, setOpen]);

  const setWidthRem = React.useCallback((rem: number) => {
    const clamped = Math.min(Math.max(rem, SIDE_MENU_MIN_WIDTH_REM), SIDE_MENU_MAX_WIDTH_REM);
    wrapperRef.current?.style.setProperty("--sidebar-width", `${clamped}rem`);
    localStorage.setItem(SIDEBAR_STORAGE_KEY_WIDTH, String(clamped));
  }, []);

  React.useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === SIDEBAR_KEYBOARD_SHORTCUT && (event.metaKey || event.ctrlKey)) {
        event.preventDefault();
        toggleSidebar();
      }
    };
    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [toggleSidebar]);

  // Auto-collapse when the viewport drops below `xl`, restore user preference when it grows back.
  // Below `xl` the sidebar overlays content (rarely desirable on narrow screens). Transitions
  // use setInternalOpen directly so they do NOT touch the persisted preference.
  React.useEffect(() => {
    if (typeof window === "undefined") {
      return;
    }
    const mediaQuery = window.matchMedia(MEDIA_QUERIES.xl);
    const handleChange = (event: MediaQueryListEvent) => {
      setInternalOpen(event.matches ? persistedOpenRef.current : false);
    };
    mediaQuery.addEventListener("change", handleChange);
    return () => mediaQuery.removeEventListener("change", handleChange);
  }, []);

  // Auto-close the mobile menu on navigation. Menu items inside federated content (e.g. Account's
  // MobileMenuContent) route through TanStack Router; relying on each item to call close() is
  // fragile and already misses the declarative `<RouterLink>` path used in AccountSideMenu.
  // Watches hash too because /components sub-items navigate by hash only.
  const location = useRouterState({ select: (s) => `${s.location.pathname}${s.location.hash}` });
  React.useEffect(() => {
    setOpenMobile(false);
  }, [location]);

  const state = open ? "expanded" : "collapsed";

  const contextValue = React.useMemo<SidebarContextProps>(
    () => ({
      state,
      open,
      setOpen,
      isMobile,
      openMobile,
      setOpenMobile,
      toggleSidebar,
      wrapperRef,
      setWidthRem
    }),
    [state, open, setOpen, isMobile, openMobile, toggleSidebar, setWidthRem]
  );

  const initialWidthRem = React.useMemo(readInitialWidthRem, []);

  const overlayValue = React.useMemo(() => ({ isOpen: openMobile, close: () => setOpenMobile(false) }), [openMobile]);

  return (
    <SidebarContext value={contextValue}>
      <collapsedContext.Provider value={state === "collapsed"}>
        <overlayContext.Provider value={overlayValue}>
          <TooltipProvider delay={0}>
            <div
              ref={wrapperRef}
              data-slot="sidebar-wrapper"
              style={
                {
                  "--sidebar-width": `${initialWidthRem}rem`,
                  "--sidebar-width-icon": `${SIDE_MENU_COLLAPSED_WIDTH_REM}rem`,
                  ...style
                } as React.CSSProperties
              }
              className={cn(
                // Subtract --banner-offset so the wrapper fits inside the viewport's content area when a banner
                // pushes body content down via padding-top. Without this, the wrapper overflows the viewport bottom
                // by --banner-offset, and any descendant pinned to the wrapper's bottom (e.g., a SidePane footer)
                // ends up below the visible frame.
                "group/sidebar-wrapper flex min-h-[calc(100svh-var(--banner-offset,0rem))] w-full has-data-[variant=inset]:bg-sidebar",
                className
              )}
              {...props}
            >
              {/* Keyboard users can jump past the sidebar to #main-content (set by AppLayout). */}
              <Link
                href="#main-content"
                variant="button-primary"
                className="absolute top-4 left-4 z-[100] -translate-y-[200%] focus-visible:translate-y-0"
              >
                {t`Skip to main content`}
              </Link>

              {children}
            </div>
          </TooltipProvider>
        </overlayContext.Provider>
      </collapsedContext.Provider>
    </SidebarContext>
  );
}

function Sidebar({
  side = "left",
  variant = "sidebar",
  collapsible = "icon",
  className,
  children,
  mobileContent,
  ...props
}: React.ComponentProps<"div"> & {
  side?: "left" | "right";
  variant?: "sidebar" | "floating" | "inset";
  collapsible?: "offcanvas" | "icon" | "none";
  // Optional override for what appears inside the full-screen mobile overlay. Consumers (e.g.
  // AccountSideMenu, MainSideMenu) pass a richer mobile navigation surface — tenant info, user
  // actions, tenant switcher, support button — that wouldn't fit in the desktop icon rail. When
  // omitted, the sidebar's regular `children` render on mobile as well.
  mobileContent?: React.ReactNode;
}) {
  const { isMobile, state, openMobile, setOpenMobile, setOpen } = useSidebar();

  if (collapsible === "none") {
    return (
      <div
        data-slot="sidebar"
        className={cn("flex h-full w-(--sidebar-width) flex-col bg-sidebar text-sidebar-foreground", className)}
        {...props}
      >
        {children}
      </div>
    );
  }

  if (isMobile) {
    return (
      <>
        {!openMobile && (
          <div
            data-slot="sidebar"
            className="fixed right-3 bottom-3 z-20 supports-[bottom:max(0px)]:bottom-[max(0.5rem,calc(env(safe-area-inset-bottom)-0.5rem))] sm:hidden"
          >
            <Button
              variant="ghost"
              size="icon"
              aria-label={t`Open navigation menu`}
              className="size-14 rounded-full border border-border bg-background shadow-lg hover:bg-hover-background focus:bg-hover-background active:bg-muted dark:hover:bg-hover-background"
              onClick={() => setOpenMobile(true)}
            >
              <MenuIcon className="size-7 text-foreground" />
            </Button>
          </div>
        )}
        {openMobile && (
          <div
            data-sidebar="sidebar"
            data-slot="sidebar"
            data-mobile="true"
            role="dialog"
            aria-modal="true"
            aria-label={t`Mobile navigation menu`}
            className="fixed top-(--banner-offset,0rem) right-0 bottom-0 left-0 z-40 flex flex-col bg-sidebar text-sidebar-foreground sm:hidden"
          >
            <div className="flex h-full w-full flex-col overflow-y-auto">{mobileContent ?? children}</div>
            <div className="fixed right-3 bottom-3 z-10 supports-[bottom:max(0px)]:bottom-[max(0.5rem,calc(env(safe-area-inset-bottom)-0.5rem))]">
              <Button
                variant="ghost"
                size="icon"
                aria-label={t`Close menu`}
                className="size-14 rounded-full border border-border bg-background/80 shadow-lg backdrop-blur-sm hover:bg-background/90 active:bg-muted"
                onClick={() => setOpenMobile(false)}
              >
                <XIcon className="size-7 text-foreground" />
              </Button>
            </div>
          </div>
        )}
      </>
    );
  }

  return (
    <div
      className="group peer hidden text-sidebar-foreground sm:block"
      data-state={state}
      data-collapsible={state === "collapsed" ? collapsible : ""}
      data-variant={variant}
      data-side={side}
      data-slot="sidebar"
    >
      {/* Backdrop: dims content behind the expanded sidebar in overlay mode (below `xl`).
          Hidden at `xl+` where the sidebar pushes content instead of overlaying. Click to collapse.
          Starts below any banner strip so banners (which push all top-positioned UI down) stay visible. */}
      <button
        type="button"
        aria-label={t`Close sidebar`}
        tabIndex={-1}
        onClick={() => setOpen(false)}
        className={cn(
          "pointer-events-none fixed top-(--banner-offset,0rem) right-0 bottom-0 left-0 z-[35] bg-black/50 opacity-0 transition-opacity duration-100 ease-linear xl:hidden",
          "group-data-[state=expanded]:pointer-events-auto group-data-[state=expanded]:opacity-100"
        )}
      />
      {/* Placeholder width:
          - Below `xl`: stays at icon-rail width so the expanded sidebar OVERLAYS content.
          - At `xl`+: follows sidebar width so main content pushes right (no overlay).
          Transitions disabled during drag via `[data-resizing]` on the wrapper. */}
      <div
        data-slot="sidebar-gap"
        className={cn(
          "relative w-(--sidebar-width-icon) bg-transparent transition-[width] duration-100 ease-linear",
          "xl:w-(--sidebar-width) xl:group-data-[collapsible=icon]:w-(--sidebar-width-icon)",
          "group-data-[collapsible=offcanvas]:w-0",
          "group-data-[side=right]:rotate-180",
          "group-data-[resizing=true]/sidebar-wrapper:transition-none"
        )}
      />
      <div
        data-slot="sidebar-container"
        className={cn(
          // Start below any banner strip (push-down behavior) and shrink the height to match so the
          // sidebar never extends past the viewport bottom. `--banner-offset` falls back to 0 when no
          // banner is mounted (see `BannerPortal`).
          "fixed top-(--banner-offset,0rem) bottom-0 z-40 hidden h-[calc(100svh-var(--banner-offset,0rem))] w-(--sidebar-width) transition-[left,right,width] duration-100 ease-linear sm:flex",
          "group-data-[resizing=true]/sidebar-wrapper:transition-none",
          side === "left"
            ? "left-0 group-data-[collapsible=offcanvas]:left-[calc(var(--sidebar-width)*-1)]"
            : "right-0 group-data-[collapsible=offcanvas]:right-[calc(var(--sidebar-width)*-1)]",
          variant === "floating" || variant === "inset"
            ? "p-2 group-data-[collapsible=icon]:w-[calc(var(--sidebar-width-icon)+(--spacing(4))+0.125rem)]"
            : "border-sidebar group-data-[collapsible=icon]:w-(--sidebar-width-icon) group-data-[side=left]:border-r group-data-[side=right]:border-l",
          className
        )}
        {...props}
      >
        <div
          data-sidebar="sidebar"
          data-slot="sidebar-inner"
          className="flex h-full w-full flex-col bg-sidebar group-data-[variant=floating]:rounded-lg group-data-[variant=floating]:border group-data-[variant=floating]:border-sidebar-border group-data-[variant=floating]:shadow-sm"
        >
          {children}
        </div>
      </div>
    </div>
  );
}

function SidebarTrigger({ className, onClick, ...props }: React.ComponentProps<typeof Button>) {
  const { toggleSidebar } = useSidebar();
  return (
    <Button
      data-sidebar="trigger"
      data-slot="sidebar-trigger"
      variant="ghost"
      size="icon"
      className={cn("size-7", className)}
      onClick={(event) => {
        onClick?.(event);
        toggleSidebar();
      }}
      aria-label={t`Toggle sidebar`}
      {...props}
    >
      <PanelLeftIcon />
    </Button>
  );
}

// Custom rail: continuous drag to resize when expanded, click toggles collapse/expand.
// A floating chevron button reveals on hover or focus (matches current SideMenu UX).
function SidebarRail({ className, ...props }: React.ComponentProps<"button">) {
  const { state, toggleSidebar, setWidthRem, wrapperRef } = useSidebar();
  const draggedRef = React.useRef(false);
  const suppressClickRef = React.useRef(false);

  // Attaches pointermove/pointerup listeners to resize the sidebar until release.
  // Only called when `state === "expanded"`. `onRelease` runs after cleanup.
  const startDrag = (startX: number, onRelease: () => void) => {
    draggedRef.current = false;
    const currentWidthRaw = wrapperRef.current?.style.getPropertyValue("--sidebar-width");
    const currentWidthRem = currentWidthRaw ? Number.parseFloat(currentWidthRaw) : SIDE_MENU_DEFAULT_WIDTH_REM;

    const handleMove = (event: PointerEvent) => {
      const deltaPx = event.clientX - startX;
      if (!draggedRef.current && Math.abs(deltaPx) > DRAG_THRESHOLD_PX) {
        draggedRef.current = true;
        // Suppress CSS width transitions during drag so the sidebar tracks the cursor 1:1.
        wrapperRef.current?.setAttribute("data-resizing", "true");
        document.body.style.cursor = "ew-resize";
      }
      if (!draggedRef.current) {
        return;
      }
      setWidthRem(currentWidthRem + deltaPx / getRootFontSize());
    };

    const handleUp = () => {
      document.body.style.cursor = "";
      wrapperRef.current?.removeAttribute("data-resizing");
      document.removeEventListener("pointermove", handleMove);
      document.removeEventListener("pointerup", handleUp);
      document.removeEventListener("pointercancel", handleUp);
      onRelease();
    };

    document.addEventListener("pointermove", handleMove);
    document.addEventListener("pointerup", handleUp);
    document.addEventListener("pointercancel", handleUp);
  };

  // Rail: click-or-drag via pointer events only (rail is tabindex=-1, no keyboard path).
  const handleRailPointerDown = (event: React.PointerEvent<HTMLButtonElement>) => {
    event.preventDefault();
    if (state !== "expanded") {
      toggleSidebar();
      return;
    }
    startDrag(event.clientX, () => {
      if (!draggedRef.current) {
        toggleSidebar();
      }
    });
  };

  // Toggle button: supports drag (expanded) AND a real onClick for keyboard.
  // `suppressClick` prevents the synthetic click (fired after pointerup) from double-toggling.
  const handleTogglePointerDown = (event: React.PointerEvent<HTMLButtonElement>) => {
    if (state !== "expanded") {
      return; // Collapsed: let onClick handle toggle (no drag available).
    }
    startDrag(event.clientX, () => {
      if (draggedRef.current) {
        suppressClickRef.current = true;
      }
    });
  };

  const handleToggleClick = () => {
    if (suppressClickRef.current) {
      suppressClickRef.current = false;
      return;
    }
    toggleSidebar();
  };

  // Arrow keys resize the sidebar when the toggle is focused (expanded state only).
  const handleToggleKeyDown = (event: React.KeyboardEvent<HTMLButtonElement>) => {
    if (state !== "expanded") {
      return;
    }
    if (event.key !== "ArrowLeft" && event.key !== "ArrowRight") {
      return;
    }
    event.preventDefault();
    const currentWidthRaw = wrapperRef.current?.style.getPropertyValue("--sidebar-width");
    const currentWidthRem = currentWidthRaw ? Number.parseFloat(currentWidthRaw) : SIDE_MENU_DEFAULT_WIDTH_REM;
    const step = event.shiftKey ? 2 : 1;
    setWidthRem(currentWidthRem + (event.key === "ArrowLeft" ? -step : step));
  };

  return (
    <>
      <button
        data-sidebar="rail"
        data-slot="sidebar-rail"
        aria-label={t`Resize sidebar`}
        tabIndex={-1}
        onPointerDown={handleRailPointerDown}
        title={t`Resize sidebar`}
        className={cn(
          "absolute inset-y-0 z-20 hidden w-4 -translate-x-1/2 transition-all ease-linear sm:flex",
          "group-data-[side=left]:-right-4 group-data-[side=right]:left-0",
          "group-data-[state=collapsed]:cursor-pointer group-data-[state=expanded]:cursor-ew-resize",
          "group-data-[collapsible=offcanvas]:translate-x-0",
          "[[data-side=left][data-collapsible=offcanvas]_&]:-right-2",
          "[[data-side=right][data-collapsible=offcanvas]_&]:-left-2",
          className
        )}
        {...props}
      />
      {/* Dedicated toggle button (keyboard-accessible). Sits above the rail in z-order so click and
          focus go to this button, while the rail behind still handles drag-to-resize on its other areas. */}
      <button
        type="button"
        data-slot="sidebar-toggle"
        aria-label={t`Toggle sidebar`}
        title={t`Toggle sidebar`}
        onPointerDown={handleTogglePointerDown}
        onClick={handleToggleClick}
        onKeyDown={handleToggleKeyDown}
        className={cn(
          // Anchored to the boundary between SidebarHeader and menu items (5rem).
          "absolute top-[var(--side-menu-collapsed-width)] right-0 z-30 hidden size-6 translate-x-1/2 -translate-y-1/2 cursor-pointer rounded-full bg-foreground p-1 text-background opacity-0 shadow-sm outline-foreground transition-[opacity,transform] duration-100 sm:block",
          "group-focus-within:opacity-100 group-hover:opacity-100 focus-visible:opacity-100 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2"
        )}
      >
        <ChevronsLeftIcon
          aria-hidden="true"
          className={cn("size-full transition-transform duration-100", "group-data-[state=collapsed]:rotate-180")}
        />
      </button>
    </>
  );
}

// SidebarInset is the main content area. Its margin-left follows the sidebar's collapsed/expanded state
// via peer-data selectors on the sibling <Sidebar>. When sidebar is expanded, main sits to its right;
// when collapsed, main shifts to sit next to the icon rail. Transitions are 200ms to match sidebar.
function SidebarInset({ className, ...props }: React.ComponentProps<"main">) {
  return (
    <main
      data-slot="sidebar-inset"
      className={cn(
        // `min-w-0` lets this flex child shrink below its content's intrinsic width, so a wide
        // sidebar (expanded or dragged larger) doesn't force the main content into horizontal overflow.
        "relative flex w-full min-w-0 flex-1 flex-col bg-background",
        "sm:peer-data-[variant=inset]:m-2 sm:peer-data-[variant=inset]:ml-0 sm:peer-data-[variant=inset]:rounded-xl sm:peer-data-[variant=inset]:shadow-sm sm:peer-data-[variant=inset]:peer-data-[state=collapsed]:ml-2",
        className
      )}
      {...props}
    />
  );
}

function SidebarHeader({ className, ...props }: React.ComponentProps<"div">) {
  return (
    <div
      data-slot="sidebar-header"
      data-sidebar="header"
      // Fixed 5rem header with content vertically centered — gives every sidebar the same "menu at
      // the top" placement (UserMenu / TenantLogo / preview avatar all align on the same row as the
      // breadcrumbs on the right). Horizontal padding stays 0 so the consumer's own inner padding
      // controls logo/avatar alignment with the menu icons below.
      className={cn("flex h-[var(--side-menu-collapsed-width)] flex-col justify-center gap-2 px-0 py-2", className)}
      {...props}
    />
  );
}

function SidebarFooter({ className, ...props }: React.ComponentProps<"div">) {
  return (
    <div
      data-slot="sidebar-footer"
      data-sidebar="footer"
      className={cn("flex flex-col gap-2 p-2", className)}
      {...props}
    />
  );
}

function SidebarSeparator({ className, ...props }: React.ComponentProps<typeof Separator>) {
  return (
    <Separator
      data-slot="sidebar-separator"
      data-sidebar="separator"
      className={cn("mx-2 w-auto bg-sidebar-border", className)}
      {...props}
    />
  );
}

function SidebarContent({ className, ...props }: React.ComponentProps<"div">) {
  return (
    <div
      data-slot="sidebar-content"
      data-sidebar="content"
      className={cn(
        "flex min-h-0 flex-1 flex-col gap-2 overflow-auto group-data-[collapsible=icon]:overflow-hidden",
        className
      )}
      {...props}
    />
  );
}

function SidebarGroup({ className, ...props }: React.ComponentProps<"div">) {
  return (
    <div
      data-slot="sidebar-group"
      data-sidebar="group"
      className={cn("relative flex w-full min-w-0 flex-col p-2", className)}
      {...props}
    />
  );
}

function SidebarGroupLabel({
  className,
  asChild = false,
  ...props
}: React.ComponentProps<"div"> & { asChild?: boolean }) {
  const Comp = asChild ? Slot : "div";
  return (
    <Comp
      data-slot="sidebar-group-label"
      data-sidebar="group-label"
      className={cn(
        // `pl-[1.375rem]` aligns the label's left edge with the menu button's icon column (SidebarMenuItem
        // mx-1 + SidebarMenuButton pl-[1.125rem] = 22px inside SidebarGroup).
        "relative flex h-8 shrink-0 items-center rounded-md pr-2 pl-[1.375rem] text-xs font-medium text-sidebar-foreground/70 uppercase outline-ring transition-opacity duration-100 ease-linear focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 [&>svg]:size-4 [&>svg]:shrink-0",
        // Keep layout height when collapsed so menu icons stay at the same vertical position. Hide the text
        // via `text-transparent` (works for raw text nodes) and render a short thick separator via `before:`.
        "group-data-[collapsible=icon]:text-transparent",
        // `left-[2rem]` is the horizontal center of the collapsed sidebar measured from the label's
        // left edge (label starts at p-2 = 0.5rem; collapsed sidebar center = --sidebar-width-icon/2 = 2.5rem → 2rem).
        // Using a fixed value (not `left-1/2`) keeps the separator from sliding as the sidebar width
        // animates during collapse/expand — the before stays pinned to the collapsed center.
        "group-data-[collapsible=icon]:before:absolute group-data-[collapsible=icon]:before:top-1/2 group-data-[collapsible=icon]:before:left-[2rem] group-data-[collapsible=icon]:before:h-1 group-data-[collapsible=icon]:before:w-6 group-data-[collapsible=icon]:before:-translate-x-1/2 group-data-[collapsible=icon]:before:-translate-y-1/2 group-data-[collapsible=icon]:before:bg-sidebar-border group-data-[collapsible=icon]:before:transition-none group-data-[collapsible=icon]:before:content-['']",
        className
      )}
      {...props}
    />
  );
}

function SidebarGroupAction({
  className,
  asChild = false,
  ...props
}: React.ComponentProps<"button"> & { asChild?: boolean }) {
  const Comp = asChild ? Slot : "button";
  return (
    <Comp
      data-slot="sidebar-group-action"
      data-sidebar="group-action"
      className={cn(
        "absolute top-3.5 right-3 flex aspect-square w-5 items-center justify-center rounded-md p-0 text-sidebar-foreground outline-ring transition-transform hover:bg-sidebar-accent hover:text-sidebar-accent-foreground focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 [&>svg]:size-4 [&>svg]:shrink-0",
        "after:absolute after:-inset-2 sm:after:hidden",
        "group-data-[collapsible=icon]:hidden",
        className
      )}
      {...props}
    />
  );
}

function SidebarGroupContent({ className, ...props }: React.ComponentProps<"div">) {
  return (
    <div
      data-slot="sidebar-group-content"
      data-sidebar="group-content"
      className={cn("w-full text-sm", className)}
      {...props}
    />
  );
}

function SidebarMenu({ className, ...props }: React.ComponentProps<"ul">) {
  return (
    <ul
      data-slot="sidebar-menu"
      data-sidebar="menu"
      className={cn("flex w-full min-w-0 flex-col gap-1", className)}
      {...props}
    />
  );
}

function SidebarMenuItem({ className, ...props }: React.ComponentProps<"li">) {
  return (
    <li
      data-slot="sidebar-menu-item"
      data-sidebar="menu-item"
      className={cn(
        // Active indicator: vertical bar flush with the sidebar's left edge (which is the browser edge).
        // Base rule shows the marker whenever this item's own button is active. Override (higher
        // specificity — more variants) hides it in expanded state when a nested sub item is active, so
        // the sub item's own marker takes over. Collapsed sub items aren't in play so the marker stays.
        "group/menu-item relative mx-1 before:pointer-events-none before:absolute before:top-[calc(var(--control-height)/2)] before:-left-3 before:h-[2rem] before:w-1 before:-translate-y-1/2 before:bg-primary before:opacity-0",
        // Matches any descendant menu-button with data-active (top-level button; sub buttons have a different
        // data-sidebar value, so they don't match this rule).
        "has-[[data-sidebar=menu-button][data-active=true]]:before:opacity-100",
        // Override: if a nested sub-button is active (in any state), the sub item's own marker takes over
        // so the parent doesn't duplicate it.
        "has-[[data-sidebar=menu-sub-button][data-active=true]]:before:opacity-0",
        className
      )}
      {...props}
    />
  );
}

// Diverges from stock ShadCN:
// - Apple HIG: uses --control-height (38px desktop / 44px mobile) instead of h-8.
// - Collapsed: keeps the SAME height as expanded so icons stay at the exact same vertical
//   position when the sidebar collapses. Only the label hides, icon centers.
// - Icon size: size-5 matches the old SideMenu visual language.
// - Active indicator: ::before pseudo-element renders a vertical bar at the sidebar's left edge
//   (-0.5rem from the item, reaching past the SidebarGroup's 0.5rem padding).
const sidebarMenuButtonVariants = cva(
  // Dim by default, brighten on hover/active. Active items get the primary-colored marker (on SidebarMenuItem)
  // so text color alone distinguishes active from hover: hover=foreground (white-ish), active=foreground+bold.
  "peer/menu-button flex w-full cursor-pointer items-center gap-4 overflow-hidden rounded-md pr-3 pl-[1.125rem] text-left text-sm text-muted-foreground outline-ring group-has-data-[sidebar=menu-action]/menu-item:pr-8 group-data-[collapsible=icon]:ml-[0.5625rem] group-data-[collapsible=icon]:w-[var(--control-height)] group-data-[collapsible=icon]:justify-center group-data-[collapsible=icon]:px-0 hover:bg-sidebar-accent hover:text-sidebar-accent-foreground focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 active:bg-sidebar-accent active:text-sidebar-accent-foreground disabled:pointer-events-none disabled:opacity-50 aria-disabled:pointer-events-none aria-disabled:opacity-50 data-[active=true]:font-medium data-[active=true]:text-foreground data-[state=open]:hover:bg-sidebar-accent data-[state=open]:hover:text-sidebar-accent-foreground [&>span:last-child]:truncate group-data-[collapsible=icon]:[&>span:last-child]:hidden [&>svg]:size-5 [&>svg]:shrink-0",
  {
    variants: {
      variant: {
        default: "hover:bg-sidebar-accent hover:text-sidebar-accent-foreground",
        outline:
          "bg-background shadow-[0_0_0_0.0625rem_var(--sidebar-border)] hover:bg-sidebar-accent hover:text-sidebar-accent-foreground hover:shadow-[0_0_0_0.0625rem_var(--sidebar-accent)]"
      },
      size: {
        default: "h-[var(--control-height)] text-sm",
        sm: "h-[var(--control-height-sm)] text-xs",
        lg: "h-[var(--control-height-lg)] text-sm"
      }
    },
    defaultVariants: {
      variant: "default",
      size: "default"
    }
  }
);

function SidebarMenuButton({
  asChild = false,
  isActive = false,
  variant = "default",
  size = "default",
  tooltip,
  className,
  ...props
}: React.ComponentProps<"button"> & {
  asChild?: boolean;
  isActive?: boolean;
  tooltip?: string | React.ComponentProps<typeof TooltipContent>;
} & VariantProps<typeof sidebarMenuButtonVariants>) {
  const Comp = asChild ? Slot : "button";
  const { isMobile, state } = useSidebar();

  const button = (
    <Comp
      data-slot="sidebar-menu-button"
      data-sidebar="menu-button"
      data-size={size}
      data-active={isActive}
      aria-current={isActive ? "page" : undefined}
      className={cn(sidebarMenuButtonVariants({ variant, size }), className)}
      {...props}
    />
  );

  // Only render the tooltip when collapsed on desktop — otherwise the label is already visible
  // next to the icon. BaseUI tooltips track their own hover state via the trigger element, so we
  // wrap the button in a `<span>` render target (asChild-style) to match the working pattern
  // used elsewhere in the codebase (e.g. UserProfileContent).
  if (!tooltip || state !== "collapsed" || isMobile) {
    return button;
  }

  const tooltipProps = typeof tooltip === "string" ? { children: tooltip } : tooltip;

  return (
    <Tooltip>
      <TooltipTrigger render={<span className="block w-full" />}>{button}</TooltipTrigger>
      <TooltipContent side="right" align="center" {...tooltipProps} />
    </Tooltip>
  );
}

function SidebarMenuAction({
  className,
  asChild = false,
  showOnHover = false,
  ...props
}: React.ComponentProps<"button"> & {
  asChild?: boolean;
  showOnHover?: boolean;
}) {
  const Comp = asChild ? Slot : "button";
  return (
    <Comp
      data-slot="sidebar-menu-action"
      data-sidebar="menu-action"
      className={cn(
        // Vertically centered on the parent menu button (--control-height = 38px / 44px mobile).
        "absolute top-[calc(var(--control-height)/2)] right-1 flex aspect-square w-5 -translate-y-1/2 cursor-pointer items-center justify-center rounded-md p-0 text-sidebar-foreground outline-ring transition-transform peer-hover/menu-button:text-sidebar-accent-foreground hover:bg-sidebar-accent hover:text-sidebar-accent-foreground focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 [&>svg]:size-4 [&>svg]:shrink-0",
        "after:absolute after:-inset-2 sm:after:hidden",
        "group-data-[collapsible=icon]:hidden",
        showOnHover &&
          "group-focus-within/menu-item:opacity-100 group-hover/menu-item:opacity-100 peer-data-[active=true]/menu-button:text-sidebar-accent-foreground data-[state=open]:opacity-100 sm:opacity-0",
        className
      )}
      {...props}
    />
  );
}

function SidebarMenuBadge({ className, ...props }: React.ComponentProps<"div">) {
  return (
    <div
      data-slot="sidebar-menu-badge"
      data-sidebar="menu-badge"
      className={cn(
        "pointer-events-none absolute top-1/2 right-1 flex h-5 min-w-5 -translate-y-1/2 items-center justify-center rounded-md px-1 text-xs font-medium text-sidebar-foreground tabular-nums select-none",
        "peer-hover/menu-button:text-sidebar-accent-foreground peer-data-[active=true]/menu-button:text-sidebar-accent-foreground",
        "group-data-[collapsible=icon]:hidden",
        className
      )}
      {...props}
    />
  );
}

function SidebarMenuSkeleton({
  className,
  showIcon = false,
  ...props
}: React.ComponentProps<"div"> & {
  showIcon?: boolean;
}) {
  const width = React.useMemo(() => {
    return `${Math.floor(Math.random() * 40) + 50}%`;
  }, []);

  return (
    <div
      data-slot="sidebar-menu-skeleton"
      data-sidebar="menu-skeleton"
      className={cn("flex h-8 items-center gap-2 rounded-md px-2", className)}
      {...props}
    >
      {showIcon && <Skeleton className="size-4 rounded-md" data-sidebar="menu-skeleton-icon" />}
      <Skeleton
        className="h-4 max-w-(--skeleton-width) flex-1"
        data-sidebar="menu-skeleton-text"
        style={{ "--skeleton-width": width } as React.CSSProperties}
      />
    </div>
  );
}

function SidebarMenuSub({ className, isExpanded, ...props }: React.ComponentProps<"ul"> & { isExpanded?: boolean }) {
  const ul = (
    <ul
      data-slot="sidebar-menu-sub"
      data-sidebar="menu-sub"
      data-state={isExpanded === undefined ? undefined : isExpanded ? "open" : "closed"}
      className={cn(
        // Expanded: `ml-[1.6875rem]` (27px) puts the left border at 40px from the sidebar edge — the same
        // column as the parent icon's center (8px group p-2 + 4px item mx-1 + 27px ml + 1px translate = 40px).
        // Sub buttons then render to the right of the connector line with their usual indent.
        // Right edge aligns with the parent item's right edge (SidebarMenuItem's mx-1 already reserves the gap).
        "ml-[1.6875rem] flex min-w-0 translate-x-px flex-col gap-1 border-l border-sidebar-border py-0.5 pr-0 pl-2.5",
        // Collapsed: drop the connector line and paint a muted band so the sub group stands out from
        // the top-level items at a glance.
        "group-data-[collapsible=icon]:mx-0 group-data-[collapsible=icon]:translate-x-0 group-data-[collapsible=icon]:rounded-md group-data-[collapsible=icon]:border-l-0 group-data-[collapsible=icon]:bg-white/60 group-data-[collapsible=icon]:px-0 group-data-[collapsible=icon]:dark:bg-white/5",
        className
      )}
      {...props}
    />
  );

  // `isExpanded` not provided → always rendered. Otherwise wrap in a grid-rows animator:
  // `0fr → 1fr` expands/collapses to natural content height; `overflow-hidden` on the inner wrapper
  // clips during the transition. The sub is always in the DOM so both entry and exit animations play.
  // Runs in both expanded and collapsed sidebar modes for consistent motion.
  if (isExpanded === undefined) {
    return ul;
  }

  return (
    <div
      data-slot="sidebar-menu-sub-wrapper"
      className={cn(
        "grid transition-[grid-template-rows] duration-200 ease-in-out",
        isExpanded ? "grid-rows-[1fr]" : "grid-rows-[0fr]"
      )}
      // When collapsed, `inert` removes the clipped sub items from the tab order and blocks
      // pointer/keyboard interaction, so Tab skips hidden entries.
      inert={!isExpanded}
    >
      {/* `overflow-hidden` clips during the height transition. `-mx-3 px-3` extends horizontally so
          the sub-item marker (sits at `-left-3`) and the right-side focus ring stay visible. `min-h-0`
          allows the wrapper to fully collapse to 0 when `grid-rows-[0fr]` — without it the sub-list's
          muted background strip bleeds through on collapsed top-level items. */}
      <div className="-mx-3 min-h-0 overflow-hidden px-3">{ul}</div>
    </div>
  );
}

function SidebarMenuSubItem({ className, ...props }: React.ComponentProps<"li">) {
  return (
    <li
      data-slot="sidebar-menu-sub-item"
      data-sidebar="menu-sub-item"
      className={cn(
        // Active indicator for sub items: centered on the SidebarMenuSub's left connector line when
        // expanded; flush with the sidebar edge (`-left-3`) when collapsed so it aligns with the
        // top-level marker column.
        // Active indicator for sub items: centered on the SidebarMenuSub's left connector line. The item
        // sits inside the ul's px-2.5 content area, so `-left-[0.78125rem]` backs the 4px marker up over
        // the 1px border so it reads as a primary-colored segment of the guide.
        // Collapsed: flush with sidebar edge via `-left-3` (aligns with the top-level marker column).
        "group/menu-sub-item relative before:pointer-events-none before:absolute before:top-[calc(var(--control-height)/2)] before:-left-[0.78125rem] before:h-[2rem] before:w-1 before:-translate-y-1/2 before:bg-primary before:opacity-0 group-data-[collapsible=icon]:before:-left-3 has-[[data-sidebar=menu-sub-button][data-active=true]]:before:opacity-100",
        className
      )}
      {...props}
    />
  );
}

// SidebarMenuCollapsible — shared single-expand coordination for menu items with nested sub groups.
// Consumers wrap their SidebarMenu (or a slice of it) in the provider and use `useSidebarMenuCollapsible`
// from each collapsible item. Only one item's sub group can be expanded at a time; expanding another
// auto-collapses the previous one. Defaults can be seeded from the current route for a sensible initial
// state. The provider doesn't persist — reset per session, matches the rest of the Sidebar UX.
type SidebarMenuCollapsibleContextValue = {
  expanded: string | null;
  toggle: (key: string) => void;
  expand: (key: string) => void;
  collapse: () => void;
};

const SidebarMenuCollapsibleContext = React.createContext<SidebarMenuCollapsibleContextValue | null>(null);

function SidebarMenuCollapsibleProvider({
  defaultExpanded = null,
  children
}: {
  defaultExpanded?: string | null;
  children: React.ReactNode;
}) {
  // Start collapsed (null) and apply `defaultExpanded` in a layout effect after mount. That way the
  // grid-rows transition in `SidebarMenuSub` plays even on the very first render of a freshly mounted
  // component (e.g., navigating between routes that remount the sidebar).
  const [expanded, setExpanded] = React.useState<string | null>(null);
  React.useLayoutEffect(() => {
    if (defaultExpanded) {
      const frame = requestAnimationFrame(() => setExpanded(defaultExpanded));
      return () => cancelAnimationFrame(frame);
    }
  }, [defaultExpanded]);

  const toggle = React.useCallback((key: string) => {
    setExpanded((prev) => (prev === key ? null : key));
  }, []);
  const expand = React.useCallback((key: string) => setExpanded(key), []);
  const collapse = React.useCallback(() => setExpanded(null), []);
  const value = React.useMemo(() => ({ expanded, toggle, expand, collapse }), [expanded, toggle, expand, collapse]);
  return <SidebarMenuCollapsibleContext value={value}>{children}</SidebarMenuCollapsibleContext>;
}

function useSidebarMenuCollapsible(key: string) {
  const context = React.useContext(SidebarMenuCollapsibleContext);
  if (!context) {
    throw new Error("useSidebarMenuCollapsible must be used within a SidebarMenuCollapsibleProvider.");
  }
  // Stabilize returned callbacks via refs. Without this, `toggle`/`expand` take a new reference every
  // time the provider's value updates (which happens each time `expanded` changes), causing any effect
  // that depends on them to re-fire and fight manual toggles (e.g., "expand when on this page" would
  // immediately re-expand after a user collapses).
  const contextRef = React.useRef(context);
  contextRef.current = context;
  const keyRef = React.useRef(key);
  keyRef.current = key;
  const stable = React.useMemo(
    () => ({
      toggle: () => contextRef.current.toggle(keyRef.current),
      expand: () => contextRef.current.expand(keyRef.current),
      collapse: () => contextRef.current.collapse()
    }),
    []
  );
  return {
    isExpanded: context.expanded === key,
    ...stable
  };
}

// SidebarMenuFlyout — hover popover with sub items for collapsed sidebars. Wraps a trigger (usually
// SidebarMenuButton) and opens a panel to the right when the sidebar is in icon-only mode. In
// expanded mode or on mobile (where labels are already visible) it's transparent and just renders
// the children. Pass `disabled` to skip the flyout when the sub items are already visible inline
// (e.g. the group is expanded as an icon band in the collapsed sidebar), so the flyout doesn't
// duplicate what's already on screen.
function SidebarMenuFlyout({
  content,
  disabled = false,
  children
}: Readonly<{
  content: React.ReactNode;
  disabled?: boolean;
  children: React.ReactNode;
}>) {
  const { state, isMobile } = useSidebar();
  if (disabled || state !== "collapsed" || isMobile) {
    return <>{children}</>;
  }
  return (
    <HoverCard>
      <HoverCardTrigger render={<span className="block w-full" />}>{children}</HoverCardTrigger>
      <HoverCardContent side="right" align="start" sideOffset={12} className="w-auto min-w-[12rem] p-1">
        {content}
      </HoverCardContent>
    </HoverCard>
  );
}

function SidebarMenuSubButton({
  asChild = false,
  size = "md",
  isActive = false,
  tooltip,
  className,
  ...props
}: React.ComponentProps<"a"> & {
  asChild?: boolean;
  size?: "sm" | "md";
  isActive?: boolean;
  tooltip?: string | React.ComponentProps<typeof TooltipContent>;
}) {
  const Comp = asChild ? Slot : "a";
  const { isMobile, state } = useSidebar();

  const button = (
    <Comp
      data-slot="sidebar-menu-sub-button"
      data-sidebar="menu-sub-button"
      data-size={size}
      data-active={isActive}
      aria-current={isActive ? "page" : undefined}
      className={cn(
        // Matches the top-level menu button height for consistency (38px desktop / 44px mobile per Apple HIG).
        // Dim by default (muted), brighten on hover/active — mirrors SidebarMenuButton styling.
        "flex h-[var(--control-height)] min-w-0 -translate-x-px cursor-pointer items-center gap-2 overflow-hidden rounded-md px-2 text-muted-foreground outline-ring hover:bg-sidebar-accent hover:text-sidebar-accent-foreground focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 active:bg-sidebar-accent active:text-sidebar-accent-foreground disabled:pointer-events-none disabled:opacity-50 aria-disabled:pointer-events-none aria-disabled:opacity-50 [&>span:last-child]:truncate [&>svg]:size-5 [&>svg]:shrink-0",
        // Active: no background fill — only marker (on SidebarMenuSubItem) + brighter text + weight.
        "data-[active=true]:font-medium data-[active=true]:text-foreground",
        size === "sm" && "text-xs",
        size === "md" && "text-sm",
        // Collapsed: render like a top-level icon button — icon-only, centered, same ml/size as SidebarMenuButton.
        // The surrounding SidebarMenuSub provides the muted background band.
        "group-data-[collapsible=icon]:ml-[0.5625rem] group-data-[collapsible=icon]:w-[var(--control-height)] group-data-[collapsible=icon]:translate-x-0 group-data-[collapsible=icon]:justify-center group-data-[collapsible=icon]:px-0",
        "group-data-[collapsible=icon]:[&>span:last-child]:hidden",
        className
      )}
      {...props}
    />
  );

  // Only render the tooltip when collapsed on desktop — the label is already visible next to the icon otherwise.
  if (!tooltip || state !== "collapsed" || isMobile) {
    return button;
  }
  const tooltipProps = typeof tooltip === "string" ? { children: tooltip } : tooltip;
  return (
    <Tooltip>
      <TooltipTrigger render={<span className="block w-full" />}>{button}</TooltipTrigger>
      <TooltipContent side="right" align="center" {...tooltipProps} />
    </Tooltip>
  );
}

export {
  collapsedContext,
  overlayContext,
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupAction,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarHeader,
  SidebarInset,
  SidebarMenu,
  SidebarMenuAction,
  SidebarMenuBadge,
  SidebarMenuButton,
  SidebarMenuCollapsibleProvider,
  SidebarMenuFlyout,
  SidebarMenuItem,
  SidebarMenuSkeleton,
  SidebarMenuSub,
  SidebarMenuSubButton,
  SidebarMenuSubItem,
  SidebarProvider,
  SidebarRail,
  SidebarSeparator,
  SidebarTrigger,
  useSidebar,
  useSidebarMenuCollapsible,
  useSidebarSafe
};
