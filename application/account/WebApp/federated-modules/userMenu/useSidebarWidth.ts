import { getRootFontSize, getSideMenuCollapsedWidth, SIDE_MENU_DEFAULT_WIDTH_REM } from "@repo/ui/utils/responsive";
import { useEffect, useState } from "react";

export function useSidebarWidth(isCollapsed: boolean) {
  const [sidebarWidth, setSidebarWidth] = useState(() => {
    if (isCollapsed) {
      return getSideMenuCollapsedWidth();
    }
    const stored = localStorage.getItem("side-menu-size");
    const widthRem = stored ? Number.parseFloat(stored) : SIDE_MENU_DEFAULT_WIDTH_REM;
    return widthRem * getRootFontSize();
  });

  useEffect(() => {
    const handleResize = (event: CustomEvent<{ widthRem: number }>) => {
      setSidebarWidth(event.detail.widthRem * getRootFontSize());
    };

    const handleToggle = (event: CustomEvent<{ isCollapsed: boolean }>) => {
      if (event.detail.isCollapsed) {
        setSidebarWidth(getSideMenuCollapsedWidth());
      } else {
        const stored = localStorage.getItem("side-menu-size");
        const widthRem = stored ? Number.parseFloat(stored) : SIDE_MENU_DEFAULT_WIDTH_REM;
        setSidebarWidth(widthRem * getRootFontSize());
      }
    };

    window.addEventListener("side-menu-resize", handleResize as EventListener);
    window.addEventListener("side-menu-toggle", handleToggle as EventListener);

    return () => {
      window.removeEventListener("side-menu-resize", handleResize as EventListener);
      window.removeEventListener("side-menu-toggle", handleToggle as EventListener);
    };
  }, []);

  useEffect(() => {
    if (isCollapsed) {
      setSidebarWidth(getSideMenuCollapsedWidth());
    } else {
      const stored = localStorage.getItem("side-menu-size");
      const widthRem = stored ? Number.parseFloat(stored) : SIDE_MENU_DEFAULT_WIDTH_REM;
      setSidebarWidth(widthRem * getRootFontSize());
    }
  }, [isCollapsed]);

  return sidebarWidth;
}
