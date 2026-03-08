import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Badge } from "@repo/ui/components/Badge";
import { Button } from "@repo/ui/components/Button";
import { Separator } from "@repo/ui/components/Separator";
import { Tooltip, TooltipContent, TooltipTrigger } from "@repo/ui/components/Tooltip";
import { formatCurrency } from "@repo/utils/currency/formatCurrency";
import { useNavigate } from "@tanstack/react-router";
import { PencilIcon } from "lucide-react";

import type { components } from "@/shared/lib/api/api.generated";
import type { SubscriptionPlan } from "@/shared/lib/api/client";

import { getPlanLabel } from "@/shared/lib/api/subscriptionPlan";

import { getFormattedPrice } from "./PlanCard";

type PlanPriceItem = components["schemas"]["PlanPriceItem"];

interface CurrentPlanSectionProps {
  currentPlan: SubscriptionPlan;
  cancelAtPeriodEnd: boolean;
  scheduledPlan: SubscriptionPlan | null;
  formattedPeriodEndLong: string | null;
  currentPriceAmount: number | null | undefined;
  currentPriceCurrency: string | null | undefined;
  plans: PlanPriceItem[] | undefined;
}

export function CurrentPlanSection({
  currentPlan,
  cancelAtPeriodEnd,
  scheduledPlan,
  formattedPeriodEndLong,
  currentPriceAmount,
  currentPriceCurrency,
  plans
}: Readonly<CurrentPlanSectionProps>) {
  const navigate = useNavigate();

  return (
    <div className="flex flex-col gap-4">
      <h3>
        <Trans>Current plan</Trans>
      </h3>
      <Separator />
      <div className="flex items-center justify-between gap-4">
        <div className="flex flex-col gap-2">
          <div className="flex flex-wrap items-center gap-3">
            <span className="font-medium">
              {getPlanLabel(currentPlan)}{" "}
              {currentPriceAmount != null && currentPriceCurrency != null
                ? t`${formatCurrency(currentPriceAmount, currentPriceCurrency)}/month`
                : getFormattedPrice(currentPlan, plans)}
            </span>
            {cancelAtPeriodEnd ? (
              <Badge variant="destructive">
                <Trans>Cancelling</Trans>
              </Badge>
            ) : (
              <Badge variant="default">
                <Trans>Active</Trans>
              </Badge>
            )}
          </div>
          {formattedPeriodEndLong && (
            <p className="text-sm text-muted-foreground">
              {cancelAtPeriodEnd ? (
                <Trans>Access until {formattedPeriodEndLong}</Trans>
              ) : (
                <Trans>Next billing date: {formattedPeriodEndLong}</Trans>
              )}
            </p>
          )}
          {scheduledPlan && !cancelAtPeriodEnd && (
            <p className="text-sm text-muted-foreground">
              <Trans>
                Changing to {getPlanLabel(scheduledPlan)} {getFormattedPrice(scheduledPlan, plans)} on{" "}
                {formattedPeriodEndLong}
              </Trans>
            </p>
          )}
        </div>
        <Tooltip>
          <TooltipTrigger
            render={
              <Button
                variant="outline"
                size="sm"
                className="shrink-0 gap-1.5"
                aria-label={t`Change plan`}
                onClick={() => navigate({ to: "/account/billing/subscription" })}
              >
                <PencilIcon className="size-4" />
                <span className="hidden sm:inline" aria-hidden="true">
                  <Trans>Change</Trans>
                </span>
              </Button>
            }
          />
          <TooltipContent className="sm:hidden">
            <Trans>Change plan</Trans>
          </TooltipContent>
        </Tooltip>
      </div>
    </div>
  );
}
