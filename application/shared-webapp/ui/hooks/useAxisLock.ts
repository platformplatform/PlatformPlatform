import { useEffect, useRef } from "react";

export function useAxisLock<T extends HTMLElement>() {
  const ref = useRef<T>(null);

  useEffect(() => {
    const element = ref.current;
    if (!element) {
      return;
    }

    let startX = 0;
    let startY = 0;
    let isLocked = false;
    const threshold = 10; // pixels before locking

    const handlePointerDown = (e: PointerEvent) => {
      if (e.pointerType !== "touch") {
        return;
      }

      startX = e.clientX;
      startY = e.clientY;
      isLocked = false;

      // Start with both directions allowed
      element.style.touchAction = "pan-x pan-y";
    };

    const handlePointerMove = (e: PointerEvent) => {
      if (e.pointerType !== "touch" || isLocked) {
        return;
      }

      const deltaX = Math.abs(e.clientX - startX);
      const deltaY = Math.abs(e.clientY - startY);

      // Lock to axis after threshold
      if (deltaX > threshold || deltaY > threshold) {
        if (deltaX > deltaY) {
          // Lock to horizontal
          element.style.touchAction = "pan-x";
        } else {
          // Lock to vertical
          element.style.touchAction = "pan-y";
        }
        isLocked = true;
      }
    };

    const handlePointerUp = () => {
      // Reset for next gesture
      element.style.touchAction = "pan-x pan-y";
      isLocked = false;
    };

    // Set initial styles
    element.style.touchAction = "pan-x pan-y";
    element.style.overscrollBehavior = "contain";

    // Use pointer events for better performance
    element.addEventListener("pointerdown", handlePointerDown);
    element.addEventListener("pointermove", handlePointerMove);
    element.addEventListener("pointerup", handlePointerUp);
    element.addEventListener("pointercancel", handlePointerUp);

    return () => {
      element.removeEventListener("pointerdown", handlePointerDown);
      element.removeEventListener("pointermove", handlePointerMove);
      element.removeEventListener("pointerup", handlePointerUp);
      element.removeEventListener("pointercancel", handlePointerUp);

      // Clean up styles
      element.style.touchAction = "";
      element.style.overscrollBehavior = "";
    };
  }, []);

  return ref;
}
