import { useRouter } from "@tanstack/react-router";
import { useEffect, useRef } from "react";
import { applicationInsights } from "./ApplicationInsightsProvider";

interface PageStaticData {
  trackingTitle?: string;
}

function getStaticData(matches: { staticData: unknown }[]): PageStaticData | undefined {
  for (let i = matches.length - 1; i >= 0; i--) {
    const data = matches[i].staticData as PageStaticData;
    if (data.trackingTitle) {
      return data;
    }
  }
  return undefined;
}

export function PageTracker() {
  const router = useRouter();
  const lastTrackedPathname = useRef<string>("");

  useEffect(() => {
    function trackPage(pathname: string, uri: string) {
      if (pathname === lastTrackedPathname.current) {
        return;
      }
      lastTrackedPathname.current = pathname;

      const data = getStaticData(router.state.matches);
      if (!data?.trackingTitle) {
        return;
      }

      applicationInsights.trackPageView({ name: data.trackingTitle, uri, properties: { type: "page" } });
    }

    trackPage(router.state.location.pathname, window.location.href);

    const unsubscribe = router.subscribe("onLoad", ({ toLocation }) => {
      trackPage(toLocation.pathname, toLocation.href);
    });

    return unsubscribe;
  }, [router]);

  return null;
}
