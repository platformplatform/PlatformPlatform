import type React from "react";
import { useEffect } from "react";
import { useSideMenuLayout } from "../hooks/useSideMenuLayout";

type AppLayoutVariant = "full" | "center";

type AppLayoutProps = {
  children: React.ReactNode;
  topMenu: React.ReactNode;
  variant?: AppLayoutVariant;
  maxWidth?: string;
  sidePane?: React.ReactNode;
  paddingBottom?: string;
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
export function AppLayout({
  children,
  topMenu,
  variant = "full",
  maxWidth = "640px",
  sidePane
}: Readonly<AppLayoutProps>) {
  const { className, style, isOverlayOpen, isMobileMenuOpen } = useSideMenuLayout();

  // Prevent body scroll when overlay is open
  useEffect(() => {
    if (isOverlayOpen) {
      document.body.style.overflow = "hidden";
    } else {
      document.body.style.overflow = "";
    }

    return () => {
      document.body.style.overflow = "";
    };
  }, [isOverlayOpen]);

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
          className={
            "flex min-h-0 w-full flex-1 flex-col overflow-y-auto p-4 pb-4 transition-all duration-100 ease-in-out supports-[padding:max(0px)]:pb-[max(1rem,env(safe-area-inset-bottom))] sm:pt-28"
          }
          style={{
            WebkitOverflowScrolling: "touch",
            overscrollBehavior: "none"
          }}
          id="main-content"
          aria-label="Main content"
          tabIndex={-1}
        >
          {variant === "center" ? (
            <div className="flex w-full flex-col items-center">
              <div className="w-full" style={{ maxWidth }}>
                {children}
              </div>
            </div>
          ) : (
            children
          )}
        </main>

        {/* Side pane area - responsive behavior */}
        {sidePane && (
          <aside
            className="fixed inset-0 z-[60] md:inset-auto md:top-[72px] md:right-0 md:h-[calc(100vh-72px)] md:w-96"
            aria-label="Side panel"
          >
            {sidePane}
          </aside>
        )}
      </div>
    </div>
  );
}
