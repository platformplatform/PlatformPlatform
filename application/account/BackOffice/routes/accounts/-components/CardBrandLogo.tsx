import { CreditCardIcon } from "lucide-react";

// Stripe returns lowercase brand identifiers (visa, mastercard, amex, discover, diners, jcb, unionpay,
// unknown, link). Render Visa and Mastercard with their wordmarks; fall back to a generic icon plus
// capitalized brand for everything else.
export function CardBrandLogo({ brand }: Readonly<{ brand: string }>) {
  const normalized = brand.toLowerCase();
  if (normalized === "visa") {
    return (
      <span className="inline-flex h-5 items-center rounded-sm bg-[#1A1F71] px-1.5 text-[0.625rem] leading-none font-bold tracking-wider text-white uppercase">
        VISA
      </span>
    );
  }
  if (normalized === "mastercard") {
    return (
      <span role="img" className="inline-flex items-center" aria-label="Mastercard">
        <span className="block size-3.5 rounded-full bg-[#EB001B]" />
        <span className="-ml-1.5 block size-3.5 rounded-full bg-[#F79E1B] mix-blend-multiply" />
      </span>
    );
  }
  return (
    <span className="inline-flex items-center gap-1">
      <CreditCardIcon className="size-4" aria-hidden={true} />
      <span className="capitalize">{brand}</span>
    </span>
  );
}
