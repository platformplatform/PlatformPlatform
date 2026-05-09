import { t } from "@lingui/core/macro";
import React, { useEffect, useRef, useState } from "react";

import { cn } from "../utils";
import { getSideMenuCollapsedWidth } from "../utils/responsive";

const appName = document.title;

type AppLayoutVariant = "full" | "center";

type AppLayoutProps = {
  children: React.ReactNode;
  variant?: AppLayoutVariant;
  maxWidth?: string;
  balanceWidth?: string;
  sidePane?: React.ReactNode;
  beforeHeader?: React.ReactNode;
  browserTitle?: string;
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
    scrollElement.addEventListener("scroll", handleScroll, { passive: true });
    handleScroll();

    return () => {
      window.removeEventListener("resize", updateTitleHeight);
      scrollElement.removeEventListener("scroll", handleScroll);
    };
  }, [enabled, contentRef]);

  return { isFullyScrolled };
}

// Shared wrapper for the slot above the h1 title (e.g. breadcrumbs). Negative top margin pulls it up toward the top of the
// header area so the slot sits visually tight to the edge without inflating the header's total height. Hidden on mobile
// because typical consumers (breadcrumbs) already hide their own content at that breakpoint, leaving the wrapper as wasted
// vertical space -- collapsing it here reclaims that area without forcing every consumer to pass a responsive class.
function BeforeHeader({ children }: { children: React.ReactNode }) {
  return <div className="-mt-2.5 mb-3 hidden h-11 items-center sm:-mt-3.5 sm:flex">{children}</div>;
}

interface HeaderContentProps {
  title: React.ReactNode;
  subtitle?: React.ReactNode;
  beforeHeader?: React.ReactNode;
  isSticky: boolean;
}

const HeaderContent = React.forwardRef<HTMLDivElement, HeaderContentProps>(
  ({ title, subtitle, beforeHeader, isSticky }, ref) => (
    <div ref={ref} className="mb-4">
      {beforeHeader && <BeforeHeader>{beforeHeader}</BeforeHeader>}
      <h1 className={cn("transition-opacity duration-200", isSticky ? "opacity-0 sm:opacity-100" : "opacity-100")}>
        {title}
      </h1>
      {subtitle && (
        <p
          className={cn(
            "mt-2 mb-0 text-muted-foreground",
            "transition-opacity duration-200",
            isSticky ? "opacity-0 sm:opacity-100" : "opacity-100"
          )}
        >
          {subtitle}
        </p>
      )}
    </div>
  )
);

HeaderContent.displayName = "HeaderContent";

interface ScrollAwayContentProps {
  title: React.ReactNode;
  subtitle?: React.ReactNode;
  beforeHeader?: React.ReactNode;
  headerRef: React.RefObject<HTMLDivElement | null>;
  children: React.ReactNode;
  variant: AppLayoutVariant;
  maxWidth: string;
  balanceWidth?: string;
}

function ScrollAwayContent({
  title,
  subtitle,
  beforeHeader,
  headerRef,
  children,
  variant,
  maxWidth,
  balanceWidth
}: ScrollAwayContentProps) {
  const wrappedChildren = balanceWidth ? (
    <div className="w-full" style={{ maxWidth: `calc(${maxWidth} - ${balanceWidth})` }}>
      {children}
    </div>
  ) : (
    children
  );

  const content = (
    <>
      {/* Header - scrolls naturally with content */}
      <div ref={headerRef} className="scroll-away-header mb-4">
        {beforeHeader && <BeforeHeader>{beforeHeader}</BeforeHeader>}
        <h1>{title}</h1>
        {subtitle && <p className="mt-2 mb-0 text-muted-foreground">{subtitle}</p>}
      </div>

      {wrappedChildren}
    </>
  );

  if (variant === "center") {
    return (
      <div className="flex min-h-0 w-full flex-1 flex-col items-center">
        <div className="flex min-h-0 w-full flex-1 flex-col" style={{ maxWidth }}>
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
  balanceWidth?: string;
  title?: React.ReactNode;
  subtitle?: React.ReactNode;
  beforeHeader?: React.ReactNode;
  headerRef: React.RefObject<HTMLDivElement | null>;
  isSticky: boolean;
  children: React.ReactNode;
}

function StandardContent({
  variant,
  maxWidth,
  balanceWidth,
  title,
  subtitle,
  beforeHeader,
  headerRef,
  isSticky,
  children
}: StandardContentProps) {
  const wrappedChildren = balanceWidth ? (
    <div className="w-full" style={{ maxWidth: `calc(${maxWidth} - ${balanceWidth})` }}>
      {children}
    </div>
  ) : (
    children
  );

  if (variant === "center") {
    return (
      <div className="flex min-h-0 w-full flex-1 flex-col items-center">
        <div className="flex min-h-0 w-full flex-1 flex-col" style={{ maxWidth }}>
          {title && (
            <HeaderContent
              ref={headerRef}
              title={title}
              subtitle={subtitle}
              beforeHeader={beforeHeader}
              isSticky={isSticky}
            />
          )}
          {wrappedChildren}
        </div>
      </div>
    );
  }

  return (
    <>
      {title && (
        <HeaderContent
          ref={headerRef}
          title={title}
          subtitle={subtitle}
          beforeHeader={beforeHeader}
          isSticky={isSticky}
        />
      )}
      {wrappedChildren}
    </>
  );
}

function ScrollEndSpacer() {
  return <div className="h-4 shrink-0 max-sm:h-18" aria-hidden="true" />;
}

export function AppLayout({
  children,
  variant = "full",
  maxWidth = "40rem",
  balanceWidth,
  sidePane,
  beforeHeader,
  title,
  browserTitle,
  subtitle,
  scrollAwayHeader = true
}: Readonly<AppLayoutProps>) {
  // Assumes rendered inside a <SidebarProvider> + <SidebarInset>, which owns the sidebar/content
  // split. AppLayout only handles the main content column (header, scroll behavior, side pane).
  const headerRef = useRef<HTMLDivElement>(null);
  const contentRef = useRef<HTMLDivElement>(null);

  const isSticky = useStickyHeader(!!title && !scrollAwayHeader, headerRef);
  const { isFullyScrolled } = useScrollAwayHeader(scrollAwayHeader && !!title, contentRef);

  useEffect(() => {
    const effectiveTitle = browserTitle ?? (typeof title === "string" ? title : undefined);
    if (effectiveTitle) {
      document.title = `${effectiveTitle} | ${appName}`;
    }
  }, [browserTitle, title]);

  return (
    <div className="flex min-h-[calc(100dvh-var(--banner-offset,0rem))] flex-1 flex-col">
      {/* Row layout: main takes remaining width, sidePane reserves its own column.
          When the pane opens, main naturally shrinks because it has `min-w-0 flex-1`. */}
      <div className="flex min-h-0 flex-1 overflow-hidden">
        {/* Mobile sticky header - appears when page header scrolls out of view */}
        {/* z-30 to stack above sticky content (z-10) like toolbars and table headers */}
        {title && (
          <div
            className={cn(
              "fixed top-(--banner-offset,0rem) right-0 left-0 z-30 border-b border-border bg-background px-[16px] py-3",
              "flex flex-col items-center justify-center text-center sm:hidden",
              "transform transition-all duration-200",
              (scrollAwayHeader ? isFullyScrolled : isSticky)
                ? "translate-y-0 opacity-100"
                : "-translate-y-full opacity-0"
            )}
            aria-hidden={scrollAwayHeader ? !isFullyScrolled : !isSticky}
          >
            <div className="max-w-[80%] truncate text-sm font-medium">{title}</div>
          </div>
        )}

        {/* Main content area */}
        {/* NOTE: horizontal padding is intentionally in px (not rem). Outer margins should stay fixed when the user */}
        {/* scales UI via --zoom-level -- growing content should consume extra space inward, not push against the viewport edges. */}
        <main
          ref={contentRef}
          className="flex min-h-0 w-full min-w-0 flex-1 flex-col overflow-y-auto bg-background px-[16px] pt-4 transition-all duration-100 ease-in-out [-webkit-overflow-scrolling:touch] focus:outline-none sm:px-[32px] sm:pt-8"
          id="main-content"
          aria-label={t`Main content`}
          tabIndex={-1}
        >
          {scrollAwayHeader && title ? (
            <ScrollAwayContent
              title={title}
              subtitle={subtitle}
              beforeHeader={beforeHeader}
              headerRef={headerRef}
              variant={variant}
              maxWidth={maxWidth}
              balanceWidth={balanceWidth}
            >
              {children}
              <ScrollEndSpacer />
            </ScrollAwayContent>
          ) : (
            <StandardContent
              variant={variant}
              maxWidth={maxWidth}
              balanceWidth={balanceWidth}
              title={title}
              subtitle={subtitle}
              beforeHeader={beforeHeader}
              headerRef={headerRef}
              isSticky={isSticky}
            >
              {children}
              <ScrollEndSpacer />
            </StandardContent>
          )}
        </main>

        {/* SidePane handles its own docking/overlay — renders as a fixed-position aside. */}
        {sidePane}
      </div>
    </div>
  );
}
