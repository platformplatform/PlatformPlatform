import { Trans } from "@lingui/react/macro";
import { CreditCardIcon } from "lucide-react";
import type { components } from "@/shared/lib/api/api.generated";

type PaymentMethod = components["schemas"]["PaymentMethod"];

type PaymentMethodDisplayProps = {
  paymentMethod: PaymentMethod | null | undefined;
};

export function PaymentMethodDisplay({ paymentMethod }: Readonly<PaymentMethodDisplayProps>) {
  if (!paymentMethod) {
    return (
      <p className="text-muted-foreground text-sm">
        <Trans>No payment method on file</Trans>
      </p>
    );
  }

  if (paymentMethod.brand === "link") {
    return (
      <div className="flex items-center gap-2 text-sm">
        <CreditCardIcon className="size-4 text-muted-foreground" />
        <span>
          <Trans>Stripe Link</Trans>
        </span>
      </div>
    );
  }

  const brandName = paymentMethod.brand.charAt(0).toUpperCase() + paymentMethod.brand.slice(1);
  const expirationDate = `${paymentMethod.expMonth.toString().padStart(2, "0")}/${paymentMethod.expYear}`;

  return (
    <div className="flex items-center gap-2 text-sm">
      <CreditCardIcon className="size-4 text-muted-foreground" />
      <span>
        {brandName} •••• {paymentMethod.last4} <Trans>expires</Trans> {expirationDate}
      </span>
    </div>
  );
}
