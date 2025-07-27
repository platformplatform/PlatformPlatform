import { useEffect, useState } from "react";
import { isMobileViewport } from "../utils/responsive";

export function useViewportResize() {
  const [isMobile, setIsMobile] = useState(isMobileViewport());

  useEffect(() => {
    const handleResize = () => {
      setIsMobile(isMobileViewport());
    };

    window.addEventListener("resize", handleResize);
    return () => window.removeEventListener("resize", handleResize);
  }, []);

  return isMobile;
}
