import { Share, X } from "lucide-react";
import { useEffect, useRef, useState } from "react";
import { Button } from "./Button";

const STORAGE_KEY = "add-to-homescreen-dismissed";
const DISMISS_DURATION_DAYS = 7;

export function AddToHomescreen() {
  const [showPrompt, setShowPrompt] = useState(false);
  const [isStandalone, setIsStandalone] = useState(false);
  const [translateY, setTranslateY] = useState(0);
  const [isDragging, setIsDragging] = useState(false);
  const startY = useRef(0);
  const currentY = useRef(0);

  useEffect(() => {
    // More comprehensive iOS detection including iPad on iOS 13+
    const isIos =
      (/iPad|iPhone|iPod/.test(navigator.userAgent) && !("MSStream" in window)) ||
      (navigator.userAgent.includes("Mac") && "ontouchend" in document);

    const isPwa =
      window.matchMedia("(display-mode: standalone)").matches ||
      ("standalone" in window.navigator && (window.navigator as unknown as { standalone?: boolean }).standalone) ||
      false;

    setIsStandalone(isPwa);

    if (isIos && !isPwa) {
      const dismissedUntil = localStorage.getItem(STORAGE_KEY);
      const isPermanentlyDismissed = dismissedUntil && Number.parseInt(dismissedUntil, 10) > Date.now();
      const isSessionDismissed = localStorage.getItem(`${STORAGE_KEY}_session`) === "true";

      if (!isPermanentlyDismissed && !isSessionDismissed) {
        setShowPrompt(true);
      }
    }
  }, []);

  const handleDismiss = () => {
    setShowPrompt(false);
    // Store dismissal timestamp for 7 days when X button is clicked
    const dismissUntil = Date.now() + DISMISS_DURATION_DAYS * 24 * 60 * 60 * 1000;
    localStorage.setItem(STORAGE_KEY, dismissUntil.toString());
  };

  const handleTouchStart = (e: React.TouchEvent) => {
    setIsDragging(true);
    startY.current = e.touches[0].clientY;
  };

  const handleTouchMove = (e: React.TouchEvent) => {
    if (!isDragging) {
      return;
    }

    currentY.current = e.touches[0].clientY;
    const diff = currentY.current - startY.current;

    // Only allow upward swipes
    if (diff < 0) {
      setTranslateY(diff);
    }
  };

  const handleTouchEnd = () => {
    setIsDragging(false);

    // If swiped up more than 50px, dismiss for current session only
    if (translateY < -50) {
      setShowPrompt(false);
      // Store session-only dismissal (no expiration, just for this session)
      localStorage.setItem(`${STORAGE_KEY}_session`, "true");
    } else {
      // Snap back
      setTranslateY(0);
    }
  };

  if (!showPrompt || isStandalone) {
    return null;
  }

  return (
    <div
      className="slide-in-from-top-2 fixed top-0 right-0 left-0 z-40 animate-in"
      style={{
        transform: `translateY(${translateY}px)`,
        transition: isDragging ? "none" : "transform 0.2s ease-out"
      }}
      onTouchStart={handleTouchStart}
      onTouchMove={handleTouchMove}
      onTouchEnd={handleTouchEnd}
    >
      <div className="border-b bg-background/95 shadow-sm backdrop-blur supports-[backdrop-filter]:bg-background/80">
        <div className="container mx-auto flex items-center justify-between gap-3 px-4 py-4">
          <img src="/apple-touch-icon.png" alt="PlatformPlatform" className="size-10 rounded-lg shadow-sm" />
          <div className="flex-1">
            <h5>Install PlatformPlatform</h5>
            <p className="text-muted-foreground text-xs">
              Add to your home screen for a faster, app-like experience. Tap <Share className="mx-0.5 inline size-3" />{" "}
              then "Add to Home Screen"
            </p>
          </div>
          <Button variant="ghost" size="icon" className="size-8 shrink-0" onClick={handleDismiss}>
            <X className="size-4" />
          </Button>
        </div>
      </div>
    </div>
  );
}
