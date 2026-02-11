import { useState } from "react";
import { createPortal } from "react-dom";
import InvitationBanner from "./InvitationBanner";
import PastDueBanner from "./PastDueBanner";
import "@repo/ui/tailwind.css";

export default function Banners() {
  const [target] = useState(() => document.getElementById("banner-root"));

  if (!target) {
    return null;
  }

  return createPortal(
    <>
      <InvitationBanner />
      <PastDueBanner />
    </>,
    target
  );
}
