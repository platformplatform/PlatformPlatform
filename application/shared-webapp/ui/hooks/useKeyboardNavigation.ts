import { useEffect, useState } from "react";

export function useKeyboardNavigation() {
  const [isKeyboardNavigation, setIsKeyboardNavigation] = useState(false);

  useEffect(() => {
    const handleKeyDown = () => setIsKeyboardNavigation(true);
    const handlePointerInput = () => setIsKeyboardNavigation(false);

    // Use capture phase to ensure we set the flag before any click handlers
    document.addEventListener("keydown", handleKeyDown, true);
    document.addEventListener("mousedown", handlePointerInput, true);
    document.addEventListener("pointerdown", handlePointerInput, true);

    return () => {
      document.removeEventListener("keydown", handleKeyDown, true);
      document.removeEventListener("mousedown", handlePointerInput, true);
      document.removeEventListener("pointerdown", handlePointerInput, true);
    };
  }, []);

  return isKeyboardNavigation;
}
