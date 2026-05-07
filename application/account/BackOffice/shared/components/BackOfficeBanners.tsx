import { useEffect, useState } from "react";
import { createPortal } from "react-dom";

import { BillingDriftBanner } from "./BillingDriftBanner";
import { MrrMismatchBanner } from "./MrrMismatchBanner";
import { UnsyncedAccountsBanner } from "./UnsyncedAccountsBanner";

/**
 * Portals all back-office banners into the fixed-top BannerPortal target so they render above the
 * sidebar and content rather than being clipped by the layout. The user-facing Banners federated
 * module relies on a lazy boundary to defer mount until BannerPortal's DOM is committed; we render
 * synchronously, so the target lookup runs in useEffect to avoid the first-render race.
 */
export function BackOfficeBanners() {
  const [target, setTarget] = useState<HTMLElement | null>(null);

  useEffect(() => {
    setTarget(document.getElementById("banner-root"));
  }, []);

  if (!target) {
    return null;
  }

  return createPortal(
    <>
      <UnsyncedAccountsBanner />
      <MrrMismatchBanner />
      <BillingDriftBanner />
    </>,
    target
  );
}
