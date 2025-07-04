import { useEffect, useState } from "react";
import { MEDIA_QUERIES } from "../utils/responsive";

/**
 * Hook to determine responsive menu behavior based on screen size
 * @returns Configuration object for responsive menu behavior
 */
export function useResponsiveMenu() {
  const [isSmallScreen, setIsSmallScreen] = useState(false);
  const [isLargeScreen, setIsLargeScreen] = useState(false);

  useEffect(() => {
    // Check initial values
    const smQuery = window.matchMedia(MEDIA_QUERIES.sm);
    const xlQuery = window.matchMedia(MEDIA_QUERIES.xl);

    setIsSmallScreen(smQuery.matches);
    setIsLargeScreen(xlQuery.matches);

    // Set up listeners
    const handleSmChange = (e: MediaQueryListEvent) => setIsSmallScreen(e.matches);
    const handleXlChange = (e: MediaQueryListEvent) => setIsLargeScreen(e.matches);

    smQuery.addEventListener("change", handleSmChange);
    xlQuery.addEventListener("change", handleXlChange);

    return () => {
      smQuery.removeEventListener("change", handleSmChange);
      xlQuery.removeEventListener("change", handleXlChange);
    };
  }, []);

  // Determine menu behavior based on screen size
  const isHidden = !isSmallScreen; // < 640px: hidden
  const forceCollapsed = isSmallScreen && !isLargeScreen; // 640px - 1279px: force collapsed
  const overlayMode = isSmallScreen && !isLargeScreen; // 640px - 1279px: overlay mode
  const className = isHidden ? "hidden" : "flex";

  return { className, forceCollapsed, overlayMode, isHidden };
}
