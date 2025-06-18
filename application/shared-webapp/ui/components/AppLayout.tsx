import type React from "react";
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
  const { className, style, isOverlayOpen } = useSideMenuLayout();

  return (
    <div
      className={className}
      style={style}
      aria-hidden={isOverlayOpen ? "true" : undefined}
      {...(isOverlayOpen ? { tabIndex: -1 } : {})}
    >
      {/* Fixed TopMenu */}
      <div className="fixed top-0 right-0 left-0 z-30 bg-background px-4 py-3" style={{ marginLeft: style.marginLeft }}>
        {topMenu}
      </div>

      {/* Scrollable content area */}
      <div className="flex-1 overflow-y-auto pt-[60px]">{children}</div>
    </div>
  );
}
