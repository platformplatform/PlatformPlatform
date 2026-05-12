import { CreditCardIcon } from "lucide-react";

import amexLogo from "@/shared/images/card-brands/amex.svg";
import discoverLogo from "@/shared/images/card-brands/discover.svg";
import mastercardLogo from "@/shared/images/card-brands/mastercard.svg";
import visaLogo from "@/shared/images/card-brands/visa.svg";

// Stripe returns lowercase brand identifiers (visa, mastercard, amex, discover, diners, jcb, unionpay,
// unknown, link). Render known brands with their wordmarks; fall back to a generic icon plus capitalized
// brand for everything else.
const brandLogos: Record<string, { src: string; alt: string }> = {
  visa: { src: visaLogo, alt: "Visa" },
  mastercard: { src: mastercardLogo, alt: "Mastercard" },
  amex: { src: amexLogo, alt: "American Express" },
  discover: { src: discoverLogo, alt: "Discover" }
};

type CardBrandLogoSize = "md" | "lg";

const sizeClassByVariant: Record<CardBrandLogoSize, string> = {
  md: "h-5 w-8",
  lg: "h-9 w-[3.5rem]"
};

export function CardBrandLogo({ brand, size = "md" }: Readonly<{ brand: string; size?: CardBrandLogoSize }>) {
  const logo = brandLogos[brand.toLowerCase()];
  if (logo) {
    return <img src={logo.src} alt={logo.alt} className={`${sizeClassByVariant[size]} rounded-sm`} />;
  }
  return (
    <span className="inline-flex items-center gap-1">
      <CreditCardIcon className="size-4" aria-hidden={true} />
      <span className="capitalize">{brand}</span>
    </span>
  );
}
