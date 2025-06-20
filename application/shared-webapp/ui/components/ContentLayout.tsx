import type React from "react";
import { useEffect, useState } from "react";
import { cn } from "../cn";
import { MEDIA_QUERIES } from "../utils/responsive";

type ContentLayoutVariant = "full" | "center";

type ContentLayoutProps = {
  children: React.ReactNode;
  variant?: ContentLayoutVariant;
  maxWidth?: string;
  className?: string;
};

/**
 * ContentLayout provides consistent content width management for application pages.
 *
 * Variants:
 * - full: Content takes full width with standard padding
 * - center: Content is always centered with configurable max width (default: 640px). When SideMenu is expanded on large screens, content is shifted 50px left for better visual balance.
 */
export function ContentLayout({
  children,
  variant = "full",
  maxWidth = "640px",
  className
}: Readonly<ContentLayoutProps>) {
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

  const baseClasses = "flex w-full flex-col gap-4 px-4 py-3";

  if (variant === "center") {
    // Shift left when on large screen and side menu is expanded (not collapsed)
    const shouldShiftLeft = isLargeScreen && !isSideMenuCollapsed;

    return (
      <div className={cn(baseClasses, "items-center", className)}>
        <div
          className="w-full"
          style={{
            maxWidth,
            transform: shouldShiftLeft ? "translateX(-50px)" : "translateX(0)"
          }}
        >
          {children}
        </div>
      </div>
    );
  }

  return <div className={cn(baseClasses, className)}>{children}</div>;
}
