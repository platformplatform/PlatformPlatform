import { useEffect, useState } from "react";

export function useScrollPosition() {
  const [scrollY, setScrollY] = useState<number>(0); // Start at 0

  useEffect(() => {
    function handleScroll() {
      const rootElement = document.getElementById("root");
      if (rootElement) {
        const currentScrollY = rootElement.scrollTop; // Use root element scrollTop
        setScrollY(currentScrollY);
      }
    }

    const rootElement = document.getElementById("root");
    if (rootElement) {
      rootElement.addEventListener("scroll", handleScroll, { capture: true, passive: true }); // Add scroll event listener to root element

      // Run once when the component mounts to capture the initial scroll position
      handleScroll();

      return () => {
        rootElement.removeEventListener("scroll", handleScroll, { capture: true }); // Cleanup on unmount
      };
    }
  }, []);

  return scrollY;
}
