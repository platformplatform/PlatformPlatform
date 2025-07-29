import { Share, X } from "lucide-react";
import { useEffect, useRef, useState } from "react";
import { Button } from "./Button";
import { Heading } from "./Heading";
import { Text } from "./Text";

const SESSION_COOKIE_NAME = "add_to_homescreen_dismissed";

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
      const hasSessionDismissed = document.cookie.includes(`${SESSION_COOKIE_NAME}=true`);

      if (!hasSessionDismissed) {
        setShowPrompt(true);
      }
    }
  }, []);

  const handleDismiss = () => {
    setShowPrompt(false);
    // Set persistent cookie for 7 days when X button is clicked
    const date = new Date();
    date.setTime(date.getTime() + 7 * 24 * 60 * 60 * 1000);
    document.cookie = `${SESSION_COOKIE_NAME}=true; expires=${date.toUTCString()}; path=/`;
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

    // If swiped up more than 50px, dismiss
    if (translateY < -50) {
      setShowPrompt(false);
      // Set session cookie - will expire when browser session ends
      document.cookie = `${SESSION_COOKIE_NAME}=true; path=/`;
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
      className="slide-in-from-top-2 fixed top-0 right-0 left-0 z-[80] animate-in"
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
          <img src="/apple-touch-icon.png" alt="PlatformPlatform" className="h-10 w-10 rounded-lg shadow-sm" />
          <div className="flex-1">
            <Heading size="md" className="text-sm">
              Install PlatformPlatform
            </Heading>
            <Text className="text-muted-foreground text-xs">
              Add to your home screen for a faster, app-like experience. Tap <Share className="mx-0.5 inline h-3 w-3" />{" "}
              then "Add to Home Screen"
            </Text>
          </div>
          <Button variant="ghost" size="icon" className="h-8 w-8 shrink-0" onPress={handleDismiss}>
            <X className="h-4 w-4" />
          </Button>
        </div>
      </div>
    </div>
  );
}
