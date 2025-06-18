import type React from "react";
import { useEffect } from "react";
import { useSideMenuLayout } from "../hooks/useSideMenuLayout";

type AppLayoutProps = {
  children: React.ReactNode;
  topMenu: React.ReactNode;
};

/**
 * AppLayout provides the fixed layout structure for applications with a side menu.
 * - Fixed TopMenu that doesn't scroll with content
 * - Scrollable content area that respects the side menu width
 * - Proper margin adjustments based on side menu state
 */
export function AppLayout({ children, topMenu }: Readonly<AppLayoutProps>) {
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
      className={className}
      style={style}
      // Prevent interaction with content when overlay is open, but not with the menu
      {...(isOverlayOpen && ({ inert: "" } as unknown as React.HTMLAttributes<HTMLDivElement>))}
    >
      {/* Fixed TopMenu with blur effect */}
      <div
        className={`fixed top-0 right-0 left-0 z-30 bg-background/95 px-4 py-3 backdrop-blur-sm ${
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
      <div className="flex-1 pt-[60px]">{children}</div>
    </div>
  );
}
