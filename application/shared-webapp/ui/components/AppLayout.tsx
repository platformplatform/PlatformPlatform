import React, { useEffect, useRef, useState } from "react";
import { useSideMenuLayout } from "../hooks/useSideMenuLayout";
import { cn } from "../utils";
import { getSideMenuCollapsedWidth } from "../utils/responsive";

type AppLayoutVariant = "full" | "center";

type AppLayoutProps = {
  children: React.ReactNode;
  topMenu: React.ReactNode;
  variant?: AppLayoutVariant;
  maxWidth?: string;
  sidePane?: React.ReactNode;
  paddingBottom?: string;
  title?: React.ReactNode;
  subtitle?: React.ReactNode;
  scrollAwayHeader?: boolean;
};

/**
 * AppLayout provides the fixed layout structure for applications with a side menu.
 * - Fixed TopMenu that doesn't scroll with content (contains secondary navigation/functions)
 * - Scrollable content area that respects the side menu width
 * - Proper margin adjustments based on side menu state
 * - SideMenu (rendered separately) contains the main navigation
 *
 * Accessibility landmarks:
 * - SideMenu: <nav role="navigation"> (main navigation)
 * - TopMenu: <div role="complementary"> (secondary navigation/functions)
 * - Content: <main> (main content area)
 * - SidePane: <aside> (complementary content)
 *
 * Variants:
 * - full: Content takes full width with standard padding
 * - center: Content is always centered with configurable max width (default: 40rem)
 */
function useBodyScrollLock(isLocked: boolean) {
  useEffect(() => {
    if (isLocked) {
      document.body.style.overflow = "hidden";
    } else {
      document.body.style.overflow = "";
    }

    return () => {
      document.body.style.overflow = "";
    };
  }, [isLocked]);
}

function useStickyHeader(enabled: boolean, headerRef: React.RefObject<HTMLDivElement | null>) {
  const [isSticky, setIsSticky] = useState(false);
  const observerRef = useRef<IntersectionObserver | null>(null);

  useEffect(() => {
    if (!enabled) {
      return;
    }

    const threshold = 0.1;
    const topMenuHeight = getSideMenuCollapsedWidth();

    observerRef.current = new IntersectionObserver(
      ([entry]) => {
        setIsSticky(!entry.isIntersecting);
      },
      {
        threshold,
        rootMargin: `-${topMenuHeight}px 0px 0px 0px`
      }
    );

    if (headerRef.current) {
      observerRef.current.observe(headerRef.current);
    }

    return () => {
      if (observerRef.current) {
        observerRef.current.disconnect();
      }
    };
  }, [enabled, headerRef]);

  return isSticky;
}

function useScrollAwayHeader(enabled: boolean, contentRef: React.RefObject<HTMLDivElement | null>) {
  const [isFullyScrolled, setIsFullyScrolled] = useState(false);
  const titleHeightRef = useRef(0);

  useEffect(() => {
    if (!enabled || !contentRef.current) {
      return;
    }

    const updateTitleHeight = () => {
      // Measure just the h1 title, not the subtitle - sticky header appears when title scrolls out
      const title = contentRef.current?.querySelector(".scroll-away-header h1") as HTMLElement;
      if (title) {
        titleHeightRef.current = title.offsetHeight;
      }
    };

    const handleScroll = () => {
      if (!contentRef.current) {
        return;
      }

      const scrollTop = contentRef.current.scrollTop;
      // Show sticky header when title (h1) is scrolled out of view
      const fullyScrolled = scrollTop >= titleHeightRef.current;

      setIsFullyScrolled((prev) => (prev !== fullyScrolled ? fullyScrolled : prev));
    };

    updateTitleHeight();
    window.addEventListener("resize", updateTitleHeight);

    const scrollElement = contentRef.current;
    scrollElement.addEventListener("scroll", handleScroll);
    handleScroll();

    return () => {
      window.removeEventListener("resize", updateTitleHeight);
      scrollElement.removeEventListener("scroll", handleScroll);
    };
  }, [enabled, contentRef]);

  return { isFullyScrolled };
}

interface HeaderContentProps {
  title: React.ReactNode;
  subtitle?: React.ReactNode;
  isSticky: boolean;
}

const HeaderContent = React.forwardRef<HTMLDivElement, HeaderContentProps>(({ title, subtitle, isSticky }, ref) => (
  <div ref={ref} className="mb-4">
    <h1 className={cn("transition-opacity duration-200", isSticky ? "opacity-0 sm:opacity-100" : "opacity-100")}>
      {title}
    </h1>
    {subtitle && (
      <p
        className={cn(
          "mt-2 text-muted-foreground",
          "transition-opacity duration-200",
          isSticky ? "opacity-0 sm:opacity-100" : "opacity-100"
        )}
      >
        {subtitle}
      </p>
    )}
  </div>
));

HeaderContent.displayName = "HeaderContent";

interface ScrollAwayContentProps {
  title: React.ReactNode;
  subtitle?: React.ReactNode;
  headerRef: React.RefObject<HTMLDivElement | null>;
  children: React.ReactNode;
  variant: AppLayoutVariant;
  maxWidth: string;
}

function ScrollAwayContent({ title, subtitle, headerRef, children, variant, maxWidth }: ScrollAwayContentProps) {
  const content = (
    <div className="flex h-full flex-col">
      {/* Header - scrolls naturally with content */}
      <div ref={headerRef} className="scroll-away-header mb-4">
        <h1>{title}</h1>
        {subtitle && <p className="mt-2 text-muted-foreground">{subtitle}</p>}
      </div>

      <div className="flex min-h-0 flex-1 flex-col">{children}</div>
    </div>
  );

  if (variant === "center") {
    return (
      <div className="flex h-full w-full flex-col items-center">
        <div className="flex h-full w-full flex-col" style={{ maxWidth }}>
          {content}
        </div>
      </div>
    );
  }

  return content;
}

interface StandardContentProps {
  variant: AppLayoutVariant;
  maxWidth: string;
  title?: React.ReactNode;
  subtitle?: React.ReactNode;
  headerRef: React.RefObject<HTMLDivElement | null>;
  isSticky: boolean;
  children: React.ReactNode;
}

function StandardContent({ variant, maxWidth, title, subtitle, headerRef, isSticky, children }: StandardContentProps) {
  if (variant === "center") {
    return (
      <div className="flex h-full w-full flex-col items-center">
        <div className="flex h-full w-full flex-col" style={{ maxWidth }}>
          {title && <HeaderContent ref={headerRef} title={title} subtitle={subtitle} isSticky={isSticky} />}
          <div className="flex min-h-0 flex-1 flex-col">{children}</div>
        </div>
      </div>
    );
  }

  return (
    <div className="flex h-full flex-col">
      {title && <HeaderContent ref={headerRef} title={title} subtitle={subtitle} isSticky={isSticky} />}
      <div className="flex min-h-0 flex-1 flex-col">{children}</div>
    </div>
  );
}

export function AppLayout({
  children,
  topMenu,
  variant = "full",
  maxWidth = "40rem",
  sidePane,
  title,
  subtitle,
  scrollAwayHeader = true
}: Readonly<AppLayoutProps>) {
  const { className, style, isOverlayOpen, isMobileMenuOpen } = useSideMenuLayout();
  const headerRef = useRef<HTMLDivElement>(null);
  const contentRef = useRef<HTMLDivElement>(null);

  useBodyScrollLock(isOverlayOpen);
  const isSticky = useStickyHeader(!!title && !scrollAwayHeader, headerRef);
  const { isFullyScrolled } = useScrollAwayHeader(scrollAwayHeader && !!title, contentRef);

  return (
    <div className="flex h-full flex-col">
      <div
        className={`${className} ${sidePane ? "grid grid-cols-[1fr_24rem] sm:grid" : "flex flex-col"} h-full overflow-hidden`}
        style={style}
      >
        {/* Mobile sticky header - appears when page header scrolls out of view */}
        {/* z-30 to stack above sticky content (z-10) like toolbars and table headers */}
        {title && (
          <div
            className={cn(
              "fixed top-0 right-0 left-0 z-30 border-border border-b bg-background px-4 py-3",
              "flex flex-col items-center justify-center text-center sm:hidden",
              "transform transition-all duration-200",
              (scrollAwayHeader ? isFullyScrolled : isSticky)
                ? "translate-y-0 opacity-100"
                : "-translate-y-full opacity-0"
            )}
            aria-hidden={scrollAwayHeader ? !isFullyScrolled : !isSticky}
          >
            <div className="max-w-[80%] truncate font-medium text-sm">{title}</div>
          </div>
        )}
        {/* Fixed TopMenu with blur effect - contains breadcrumbs and secondary functions */}
        {/* Height matches collapsed side menu width for visual consistency */}
        <aside
          className={`fixed top-0 right-0 left-0 z-20 h-[var(--side-menu-collapsed-width)] bg-sidebar px-4 sm:border-border sm:border-b ${
            isMobileMenuOpen ? "hidden" : ""
          } hidden sm:flex sm:items-center`}
          aria-label="Secondary navigation"
        >
          <div className="w-full" style={{ marginLeft: style.marginLeft }}>
            {topMenu}
          </div>
        </aside>

        {/* Main content area */}
        <main
          ref={contentRef}
          className={
            "flex min-h-0 w-full flex-1 flex-col overflow-y-auto bg-background px-4 pt-4 pb-0 transition-all duration-100 ease-in-out [-webkit-overflow-scrolling:touch] focus:outline-none sm:pt-28 sm:pb-4 supports-[padding:max(0px)]:sm:pb-[max(1rem,env(safe-area-inset-bottom))]"
          }
          id="main-content"
          aria-label="Main content"
          tabIndex={-1}
        >
          {scrollAwayHeader && title ? (
            <ScrollAwayContent
              title={title}
              subtitle={subtitle}
              headerRef={headerRef}
              variant={variant}
              maxWidth={maxWidth}
            >
              {children}
            </ScrollAwayContent>
          ) : (
            <StandardContent
              variant={variant}
              maxWidth={maxWidth}
              title={title}
              subtitle={subtitle}
              headerRef={headerRef}
              isSticky={isSticky}
            >
              {children}
            </StandardContent>
          )}
        </main>

        {/* Side pane area - fullscreen mode uses portal, side-by-side uses this wrapper */}
        {sidePane && (
          <aside
            className="fixed inset-0 bg-card md:inset-auto md:top-[var(--side-menu-collapsed-width)] md:right-0 md:bottom-0 md:z-10 md:w-96"
            aria-label="Side panel"
          >
            {sidePane}
          </aside>
        )}
      </div>
    </div>
  );
}
