import InvitationBanner from "./InvitationBanner";
import PastDueBanner from "./PastDueBanner";
import "@repo/ui/tailwind.css";

export default function Banners() {
  return (
    <>
      <InvitationBanner />
      <PastDueBanner />
    </>
  );
}
