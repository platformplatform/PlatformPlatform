import React, { useEffect, useRef, useState } from "react";
import { cn } from "../cn";
import { useSideMenuLayout } from "../hooks/useSideMenuLayout";

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
 * - center: Content is always centered with configurable max width (default: 640px)
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

    observerRef.current = new IntersectionObserver(
      ([entry]) => {
        setIsSticky(!entry.isIntersecting);
      },
      {
        threshold,
        rootMargin: "-60px 0px 0px 0px"
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
  const [scrollProgress, setScrollProgress] = useState(0);
  const [headerHeight, setHeaderHeight] = useState(0);
  const headerRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    if (!enabled || !contentRef.current) {
      return;
    }

    const updateHeaderHeight = () => {
      const header = contentRef.current?.querySelector(".scroll-away-header") as HTMLDivElement;
      if (header) {
        headerRef.current = header;
        // Only count the header height, not the entire content
        const headerContent = header.querySelector(".mb-4") as HTMLDivElement;
        setHeaderHeight(headerContent ? headerContent.offsetHeight : header.offsetHeight);
      }
    };

    const handleScroll = () => {
      if (!contentRef.current || !headerRef.current) {
        return;
      }

      const scrollTop = contentRef.current.scrollTop;
      const maxScroll = Math.max(headerHeight - 60, 0); // Leave 60px for sticky header
      const progress = maxScroll > 0 ? Math.min(scrollTop / maxScroll, 1) : 0;

      setScrollProgress(progress);
    };

    updateHeaderHeight();
    window.addEventListener("resize", updateHeaderHeight);

    const scrollElement = contentRef.current;
    scrollElement.addEventListener("scroll", handleScroll);
    handleScroll();

    return () => {
      window.removeEventListener("resize", updateHeaderHeight);
      scrollElement.removeEventListener("scroll", handleScroll);
    };
  }, [enabled, contentRef, headerHeight]);

  return { scrollProgress, isFullyScrolled: scrollProgress >= 1 };
}

interface HeaderContentProps {
  title: React.ReactNode;
  subtitle?: React.ReactNode;
  isSticky: boolean;
}

const HeaderContent = React.forwardRef<HTMLDivElement, HeaderContentProps>(({ title, subtitle, isSticky }, ref) => (
  <div ref={ref} className="mb-4">
    <h1
      className={cn(
        "font-semibold text-3xl",
        "transition-opacity duration-200",
        isSticky ? "opacity-0 sm:opacity-100" : "opacity-100"
      )}
    >
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
  scrollProgress: number;
  headerRef: React.RefObject<HTMLDivElement | null>;
  children: React.ReactNode;
}

function ScrollAwayContent({ title, subtitle, scrollProgress, headerRef, children }: ScrollAwayContentProps) {
  return (
    <>
      {/* Mobile version with scroll-away header */}
      <div className="flex h-full flex-col sm:hidden">
        <div
          className="scroll-away-header"
          style={{
            transform: `translateY(-${scrollProgress * 100}%)`,
            opacity: 1 - scrollProgress
          }}
        >
          <div className="mb-4">
            <h1 className="font-semibold text-3xl">{title}</h1>
            {subtitle && <p className="mt-2 text-muted-foreground">{subtitle}</p>}
          </div>
        </div>
        <div className="flex min-h-0 flex-1 flex-col">{children}</div>
      </div>

      {/* Desktop version - no scroll away behavior */}
      <div className="hidden sm:flex sm:h-full sm:flex-col">
        <div ref={headerRef} className="mb-4">
          <h1 className="font-semibold text-3xl">{title}</h1>
          {subtitle && <p className="mt-2 text-muted-foreground">{subtitle}</p>}
        </div>
        <div className="flex min-h-0 flex-1 flex-col">{children}</div>
      </div>
    </>
  );
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
  maxWidth = "640px",
  sidePane,
  title,
  subtitle,
  scrollAwayHeader = false
}: Readonly<AppLayoutProps>) {
  const { className, style, isOverlayOpen, isMobileMenuOpen } = useSideMenuLayout();
  const headerRef = useRef<HTMLDivElement>(null);
  const contentRef = useRef<HTMLDivElement>(null);

  useBodyScrollLock(isOverlayOpen);
  const isSticky = useStickyHeader(!!title && !scrollAwayHeader, headerRef);
  const { scrollProgress, isFullyScrolled } = useScrollAwayHeader(scrollAwayHeader && !!title, contentRef);

  return (
    <div className="flex h-full flex-col">
      {/* Skip navigation link for keyboard users */}
      <a
        href="#main-content"
        className="sr-only focus:not-sr-only focus:absolute focus:top-4 focus:left-4 focus:z-[100] focus:rounded-md focus:bg-primary focus:px-4 focus:py-2 focus:text-primary-foreground focus:shadow-lg"
      >
        Skip to main content
      </a>
      <div
        className={`${className} ${sidePane ? "grid grid-cols-[1fr_384px] sm:grid" : "flex flex-col"} h-full overflow-hidden`}
        style={style}
      >
        {/* Mobile sticky header - shown differently based on scroll mode */}
        {title && (
          <div
            className={cn(
              "fixed top-0 right-0 left-0 z-40 border-border border-b bg-background/95 px-4 py-3 backdrop-blur-sm",
              "flex flex-col items-center justify-center text-center sm:hidden",
              "transform transition-all duration-200",
              scrollAwayHeader
                ? isFullyScrolled
                  ? "translate-y-0 opacity-100"
                  : "-translate-y-full opacity-0"
                : isSticky
                  ? "translate-y-0 opacity-100"
                  : "-translate-y-full opacity-0"
            )}
            aria-hidden={scrollAwayHeader ? !isFullyScrolled : !isSticky}
          >
            <div className="max-w-[80%] truncate font-medium text-sm">{title}</div>
          </div>
        )}
        {/* Fixed TopMenu with blur effect - contains breadcrumbs and secondary functions */}
        <div
          role="complementary"
          className={`fixed top-0 right-0 left-0 z-30 bg-background/95 px-4 py-4 backdrop-blur-sm sm:border-border sm:border-b ${
            isMobileMenuOpen ? "hidden" : ""
          } hidden sm:block`}
          aria-label="Secondary navigation"
        >
          <div style={{ marginLeft: style.marginLeft }}>{topMenu}</div>
        </div>

        {/* Main content area */}
        <main
          ref={contentRef}
          className={
            "flex min-h-0 w-full flex-1 flex-col overflow-y-auto p-4 pt-4 pb-4 transition-all duration-100 ease-in-out [-webkit-overflow-scrolling:touch] supports-[padding:max(0px)]:pb-[max(1rem,env(safe-area-inset-bottom))] sm:pt-28"
          }
          id="main-content"
          aria-label="Main content"
          tabIndex={-1}
        >
          {scrollAwayHeader && title ? (
            <ScrollAwayContent title={title} subtitle={subtitle} scrollProgress={scrollProgress} headerRef={headerRef}>
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

        {/* Side pane area - responsive behavior */}
        {sidePane && (
          <aside
            className="fixed inset-0 z-[60] md:inset-auto md:top-[72px] md:right-0 md:bottom-0 md:w-96"
            aria-label="Side panel"
          >
            {sidePane}
          </aside>
        )}
      </div>
    </div>
  );
}
