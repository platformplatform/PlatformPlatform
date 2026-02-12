import { useState } from "react";
import { createPortal } from "react-dom";
import DisputeBanner from "./DisputeBanner";
import ExpiringCardBanner from "./ExpiringCardBanner";
import InvitationBanner from "./InvitationBanner";
import PastDueBanner from "./PastDueBanner";
import RefundBanner from "./RefundBanner";
import "@repo/ui/tailwind.css";

export default function Banners() {
  const [target] = useState(() => document.getElementById("banner-root"));

  if (!target) {
    return null;
  }

  return createPortal(
    <>
      <InvitationBanner />
      <DisputeBanner />
      <PastDueBanner />
      <ExpiringCardBanner />
      <RefundBanner />
    </>,
    target
  );
}
