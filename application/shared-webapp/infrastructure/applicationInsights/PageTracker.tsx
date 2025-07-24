import { useRouter } from "@tanstack/react-router";
import { useEffect, useRef } from "react";
import { applicationInsights } from "./ApplicationInsightsProvider";

export function PageTracker() {
  const router = useRouter();
  const lastPathname = useRef<string>("");

  useEffect(() => {
    // Track initial page view
    const pathname = router.state.location.pathname;
    if (pathname !== lastPathname.current) {
      applicationInsights.trackPageView({
        name: pathname,
        uri: window.location.href
      });
      lastPathname.current = pathname;
    }

    // Subscribe to navigation events
    const unsubscribe = router.subscribe("onLoad", ({ toLocation }) => {
      if (toLocation.pathname !== lastPathname.current) {
        applicationInsights.trackPageView({
          name: toLocation.pathname,
          uri: toLocation.href
        });
        lastPathname.current = toLocation.pathname;
      }
    });

    return unsubscribe;
  }, [router]);

  return null;
}
