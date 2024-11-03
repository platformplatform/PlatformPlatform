import { useEffect, useState } from "react";

export function useMousePosition(elementRef: React.RefObject<HTMLElement>, throttleMs = 100) {
  const [mousePosition, setMousePosition] = useState({ x: null as number | null, y: null as number | null });

  useEffect(() => {
    let lastEventTime = 0;

    const handleMouseMove = (event: MouseEvent) => {
      const now = Date.now();
      if (now - lastEventTime < throttleMs) return;
      lastEventTime = now;

      const element = elementRef.current;
      if (element) {
        const rect = element.getBoundingClientRect();
        setMousePosition({ x: event.clientX - rect.left, y: event.clientY - rect.top });
      }
    };

    const handleMouseOut = () => {
      setMousePosition({ x: null, y: null });
    };

    const element = elementRef.current;
    if (element) {
      element.addEventListener("mousemove", handleMouseMove);
      element.addEventListener("mouseout", handleMouseOut);
    }

    return () => {
      if (element) {
        element.removeEventListener("mousemove", handleMouseMove);
        element.removeEventListener("mouseout", handleMouseOut);
      }
    };
  }, [elementRef, throttleMs]);

  return mousePosition;
}
