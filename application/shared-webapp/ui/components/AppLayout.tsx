import React, { useEffect, useRef, useState } from "react";
import { useSideMenuLayout } from "../hooks/useSideMenuLayout";
import { cn } from "../utils";
import { getSideMenuCollapsedWidth } from "../utils/responsive";

type AppLayoutVariant = "full" | "center";

type AppLayoutProps = {
  children: React.ReactNode;
  variant?: AppLayoutVariant;
  maxWidth?: string;
  sidePane?: React.ReactNode;
  title?: React.ReactNode;
  subtitle?: React.ReactNode;
  scrollAwayHeader?: boolean;
};

/**
 * AppLayout provides the fixed layout structure for applications with a side menu.
 * - Scrollable content area that respects the side menu width
 * - Proper margin adjustments based on side menu state
 * - SideMenu (rendered separately) contains the main navigation
 *
 * Accessibility landmarks:
 * - SideMenu: <nav role="navigation"> (main navigation)
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
    <>
      {/* Header - scrolls naturally with content */}
      <div ref={headerRef} className="scroll-away-header mb-4">
        <h1>{title}</h1>
        {subtitle && <p className="mt-2 text-muted-foreground">{subtitle}</p>}
      </div>

      {children}
    </>
  );

  if (variant === "center") {
    return (
      <div className="flex w-full flex-col items-center">
        <div className="w-full" style={{ maxWidth }}>
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
      <div className="flex w-full flex-col items-center">
        <div className="w-full" style={{ maxWidth }}>
          {title && <HeaderContent ref={headerRef} title={title} subtitle={subtitle} isSticky={isSticky} />}
          {children}
        </div>
      </div>
    );
  }

  return (
    <>
      {title && <HeaderContent ref={headerRef} title={title} subtitle={subtitle} isSticky={isSticky} />}
      {children}
    </>
  );
}

export function AppLayout({
  children,
  variant = "full",
  maxWidth = "40rem",
  sidePane,
  title,
  subtitle,
  scrollAwayHeader = true
}: Readonly<AppLayoutProps>) {
  const { className, style, isOverlayOpen } = useSideMenuLayout();
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
              "fixed top-[calc(var(--past-due-banner-height,0rem)+var(--invitation-banner-height,0rem))] right-0 left-0 z-30 border-border border-b bg-background px-4 py-3",
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

        {/* Main content area */}
        <main
          ref={contentRef}
          className="flex min-h-0 w-full flex-1 flex-col overflow-y-auto bg-background px-4 pt-[calc(1rem+var(--past-due-banner-height,0rem)+var(--invitation-banner-height,0rem))] pb-4 transition-all duration-100 ease-in-out [-webkit-overflow-scrolling:touch] focus:outline-none sm:px-8 sm:pt-[calc(2rem+var(--past-due-banner-height,0rem)+var(--invitation-banner-height,0rem))]"
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
            className="fixed top-[calc(var(--past-due-banner-height,0rem)+var(--invitation-banner-height,0rem))] right-0 bottom-0 left-0 z-40 bg-card md:left-auto md:w-96"
            aria-label="Side panel"
          >
            {sidePane}
          </aside>
        )}
      </div>
    </div>
  );
}
