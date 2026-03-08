import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { Separator } from "@repo/ui/components/Separator";
import { Tooltip, TooltipContent, TooltipTrigger } from "@repo/ui/components/Tooltip";
import { PencilIcon } from "lucide-react";

import type { components } from "@/shared/lib/api/api.generated";

import { BillingInfoDisplay } from "./BillingInfoDisplay";

type BillingInfo = components["schemas"]["BillingInfo"];

interface BillingInfoSectionProps {
  billingInfo: BillingInfo | null | undefined;
  isStripeConfigured: boolean;
  onEditClick: () => void;
}

export function BillingInfoSection({
  billingInfo,
  isStripeConfigured,
  onEditClick
}: Readonly<BillingInfoSectionProps>) {
  return (
    <div className="mt-8 flex flex-col gap-4">
      <h3>
        <Trans>Billing information</Trans>
      </h3>
      <Separator />
      <div className="flex items-start justify-between gap-4">
        <BillingInfoDisplay billingInfo={billingInfo} />
        <Tooltip>
          <TooltipTrigger
            render={
              <Button
                variant="outline"
                size="sm"
                className="shrink-0 gap-1.5"
                aria-label={t`Edit billing information`}
                onClick={onEditClick}
                disabled={!isStripeConfigured}
              >
                <PencilIcon className="size-4" />
                <span className="hidden sm:inline" aria-hidden="true">
                  <Trans>Edit</Trans>
                </span>
              </Button>
            }
          />
          <TooltipContent className="sm:hidden">
            <Trans>Edit billing information</Trans>
          </TooltipContent>
        </Tooltip>
      </div>
    </div>
  );
}
