import { Trans } from "@lingui/react/macro";
import { MailIcon, MapPinIcon } from "lucide-react";
import type { components } from "@/shared/lib/api/api.generated";

type BillingInfo = components["schemas"]["BillingInfo"];

type BillingInfoDisplayProps = {
  billingInfo: BillingInfo | null | undefined;
};

export function BillingInfoDisplay({ billingInfo }: Readonly<BillingInfoDisplayProps>) {
  if (!billingInfo) {
    return (
      <p className="text-muted-foreground text-sm">
        <Trans>No billing information on file</Trans>
      </p>
    );
  }

  const address = billingInfo.address;
  const addressParts = [
    address?.line1,
    address?.line2,
    address?.postalCode,
    address?.city,
    address?.state,
    address?.country
  ]
    .filter(Boolean)
    .join(", ");

  return (
    <div className="flex flex-col gap-2 text-sm">
      {addressParts && (
        <div className="flex items-center gap-2">
          <MapPinIcon className="size-4 text-muted-foreground" />
          <span>{addressParts}</span>
        </div>
      )}
      {billingInfo.email && (
        <div className="flex items-center gap-2">
          <MailIcon className="size-4 text-muted-foreground" />
          <span>{billingInfo.email}</span>
        </div>
      )}
    </div>
  );
}
