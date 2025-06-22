import type React from "react";
import { useEffect, useState } from "react";
import { useSideMenuLayout } from "../hooks/useSideMenuLayout";
import { MEDIA_QUERIES } from "../utils/responsive";

type AppLayoutVariant = "full" | "center";

type AppLayoutProps = {
  children: React.ReactNode;
  topMenu: React.ReactNode;
  variant?: AppLayoutVariant;
  maxWidth?: string;
};

/**
 * AppLayout provides the fixed layout structure for applications with a side menu.
 * - Fixed TopMenu that doesn't scroll with content
 * - Scrollable content area that respects the side menu width
 * - Proper margin adjustments based on side menu state
 *
 * Variants:
 * - full: Content takes full width with standard padding
 * - center: Content is always centered with configurable max width (default: 640px). When SideMenu is expanded on large screens, content is shifted 50px left for better visual balance.
 */
export function AppLayout({ children, topMenu, variant = "full", maxWidth = "640px" }: Readonly<AppLayoutProps>) {
  const { className, style, isOverlayOpen, isMobileMenuOpen } = useSideMenuLayout();

  const [isLargeScreen, setIsLargeScreen] = useState(() =>
    typeof window !== "undefined" ? window.matchMedia(MEDIA_QUERIES.xl).matches : false
  );
  const [isSideMenuCollapsed, setIsSideMenuCollapsed] = useState(() => {
    if (typeof window === "undefined") {
      return true;
    }
    // Check if we're on large screen
    const isLarge = window.matchMedia(MEDIA_QUERIES.xl).matches;
    if (!isLarge) {
      return true; // Always collapsed on smaller screens
    }

    const stored = localStorage.getItem("side-menu-collapsed");
    return stored === "true";
  });

  // Listen for screen size changes
  useEffect(() => {
    const xlQuery = window.matchMedia(MEDIA_QUERIES.xl);
    const handleXlChange = (e: MediaQueryListEvent) => setIsLargeScreen(e.matches);

    xlQuery.addEventListener("change", handleXlChange);
    return () => xlQuery.removeEventListener("change", handleXlChange);
  }, []);

  // Listen for side menu toggle events
  useEffect(() => {
    const handleMenuToggle = (event: CustomEvent) => {
      if (isLargeScreen) {
        setIsSideMenuCollapsed(event.detail.isCollapsed);
      }
    };

    window.addEventListener("side-menu-toggle", handleMenuToggle as EventListener);
    return () => window.removeEventListener("side-menu-toggle", handleMenuToggle as EventListener);
  }, [isLargeScreen]);

  // Update side menu state when screen size changes
  useEffect(() => {
    if (!isLargeScreen) {
      setIsSideMenuCollapsed(true);
    } else {
      const stored = localStorage.getItem("side-menu-collapsed");
      setIsSideMenuCollapsed(stored === "true");
    }
  }, [isLargeScreen]);

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
      className={`${className} flex h-screen flex-col`}
      style={style}
      // Prevent interaction with content when overlay is open, but not with the menu
      {...(isOverlayOpen && ({ inert: "" } as unknown as React.HTMLAttributes<HTMLDivElement>))}
    >
      {/* Fixed TopMenu with blur effect */}
      <div
        className={`fixed top-0 right-0 left-0 z-30 bg-background/95 py-3 pr-4 pl-8 backdrop-blur-sm ${
          isMobileMenuOpen ? "hidden" : ""
        }`}
        style={{ marginLeft: style.marginLeft }}
      >
        {topMenu}
      </div>
      {/* Soft gradient fade below TopMenu */}
      <div
        className={`pointer-events-none fixed inset-x-0 top-[60px] z-30 h-4 bg-gradient-to-b from-background/30 to-transparent ${
          isMobileMenuOpen ? "hidden" : ""
        }`}
        style={{ marginLeft: style.marginLeft }}
      />

      {/* Scrollable content area with bounce */}
      <div className="flex h-full min-h-[600px] w-full flex-1 flex-col pt-24 pr-4 pl-8">
        {variant === "center" ? (
          <div className="flex w-full flex-col items-center">
            <div
              className="w-full"
              style={{
                maxWidth,
                transform: isLargeScreen && !isSideMenuCollapsed ? "translateX(-50px)" : "translateX(0)"
              }}
            >
              {children}
            </div>
          </div>
        ) : (
          children
        )}
      </div>
    </div>
  );
}
