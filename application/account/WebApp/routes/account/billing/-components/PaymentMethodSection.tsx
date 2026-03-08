import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { Separator } from "@repo/ui/components/Separator";
import { Tooltip, TooltipContent, TooltipTrigger } from "@repo/ui/components/Tooltip";
import { PencilIcon } from "lucide-react";

import type { components } from "@/shared/lib/api/api.generated";

import { PaymentMethodDisplay } from "./PaymentMethodDisplay";

type PaymentMethod = components["schemas"]["PaymentMethod"];

interface PaymentMethodSectionProps {
  paymentMethod: PaymentMethod | null | undefined;
  isStripeConfigured: boolean;
  onUpdateClick: () => void;
}

export function PaymentMethodSection({
  paymentMethod,
  isStripeConfigured,
  onUpdateClick
}: Readonly<PaymentMethodSectionProps>) {
  return (
    <div className="mt-8 flex flex-col gap-4">
      <h3>
        <Trans>Payment method</Trans>
      </h3>
      <Separator />
      <div className="flex items-center justify-between gap-4">
        <PaymentMethodDisplay paymentMethod={paymentMethod} />
        <Tooltip>
          <TooltipTrigger
            render={
              <Button
                variant="outline"
                size="sm"
                className="shrink-0 gap-1.5"
                aria-label={t`Update payment method`}
                onClick={onUpdateClick}
                disabled={!isStripeConfigured}
              >
                <PencilIcon className="size-4" />
                <span className="hidden sm:inline" aria-hidden="true">
                  <Trans>Update</Trans>
                </span>
              </Button>
            }
          />
          <TooltipContent className="sm:hidden">
            <Trans>Update payment method</Trans>
          </TooltipContent>
        </Tooltip>
      </div>
    </div>
  );
}
