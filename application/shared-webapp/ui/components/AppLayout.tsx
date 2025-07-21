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
};

/**
 * AppLayout provides the fixed layout structure for applications with a side menu.
 * - Fixed TopMenu that doesn't scroll with content
 * - Scrollable content area that respects the side menu width
 * - Proper margin adjustments based on side menu state
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
    <div
      className={`${className} ${sidePane ? "grid grid-cols-[1fr_384px] sm:grid" : "flex flex-col"} h-screen`}
      style={style}
      // Prevent interaction with content when overlay is open, but not with the menu
      {...(isOverlayOpen && ({ inert: "" } as unknown as React.HTMLAttributes<HTMLDivElement>))}
    >
      {/* Fixed TopMenu with blur effect */}
      <div
        className={`fixed top-0 right-0 left-0 z-30 bg-background/95 px-4 py-4 backdrop-blur-sm sm:border-border sm:border-b ${
          isMobileMenuOpen ? "hidden" : ""
        } hidden sm:block`}
      >
        <div style={{ marginLeft: style.marginLeft }}>{topMenu}</div>
      </div>

      {/* Main content area */}
      <div
        className={`flex h-full min-h-[600px] w-full flex-1 flex-col px-4 pt-4 transition-all duration-100 ease-in-out sm:pt-28 ${
          sidePane ? "overflow-x-auto" : ""
        }`}
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
      </div>

      {/* Side pane area - responsive behavior */}
      {sidePane && (
        <div className="fixed inset-0 z-[60] md:inset-auto md:top-[72px] md:right-0 md:h-[calc(100vh-72px)] md:w-96">
          {sidePane}
        </div>
      )}
    </div>
  );
}
