import { useState } from "react";
import { createPortal } from "react-dom";
import InvitationBanner from "./InvitationBanner";
import "@repo/ui/tailwind.css";

export default function Banners() {
  const [target] = useState(() => document.getElementById("banner-root"));

  if (!target) {
    return null;
  }

  return createPortal(<InvitationBanner />, target);
}
