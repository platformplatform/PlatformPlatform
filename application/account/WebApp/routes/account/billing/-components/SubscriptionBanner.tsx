import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { AlertTriangleIcon } from "lucide-react";

import type { SubscriptionPlan } from "@/shared/lib/api/client";

import { getPlanLabel } from "@/shared/lib/api/subscriptionPlan";

interface CancellationBannerProps {
  currentPlan: SubscriptionPlan;
  formattedPeriodEnd: string | null;
  onReactivate?: () => void;
}

export function CancellationBanner({
  currentPlan,
  formattedPeriodEnd,
  onReactivate
}: Readonly<CancellationBannerProps>) {
  return (
    <div className="mb-6 flex items-center justify-between gap-3 rounded-lg border border-border bg-muted/50 p-4 text-sm text-muted-foreground">
      <div className="flex items-center gap-3">
        <AlertTriangleIcon className="size-4 shrink-0" />
        {formattedPeriodEnd ? (
          <Trans>
            Your {getPlanLabel(currentPlan)} subscription has been cancelled and will end on {formattedPeriodEnd}.
          </Trans>
        ) : (
          <Trans>Your subscription has been cancelled and will end at the end of the current billing period.</Trans>
        )}
      </div>
      {onReactivate && (
        <Button size="sm" className="shrink-0" onClick={onReactivate}>
          <Trans>Reactivate</Trans>
        </Button>
      )}
    </div>
  );
}

interface DowngradeBannerProps {
  scheduledPlan: SubscriptionPlan;
  formattedPeriodEnd: string | null;
  onCancelDowngrade?: () => void;
}

export function DowngradeBanner({
  scheduledPlan,
  formattedPeriodEnd,
  onCancelDowngrade
}: Readonly<DowngradeBannerProps>) {
  return (
    <div className="mb-6 flex items-center justify-between gap-3 rounded-lg border border-border bg-muted/50 p-4 text-sm text-muted-foreground">
      <div className="flex items-center gap-3">
        <AlertTriangleIcon className="size-4 shrink-0" />
        {formattedPeriodEnd ? (
          <Trans>
            Your subscription will be downgraded to {getPlanLabel(scheduledPlan)} on {formattedPeriodEnd}.
          </Trans>
        ) : (
          <Trans>
            Your subscription will be downgraded to {getPlanLabel(scheduledPlan)} at the end of the current billing
            period.
          </Trans>
        )}
      </div>
      {onCancelDowngrade && (
        <Button size="sm" className="shrink-0" onClick={onCancelDowngrade}>
          <Trans>Cancel downgrade</Trans>
        </Button>
      )}
    </div>
  );
}

export function StripeNotConfiguredBanner() {
  return (
    <div className="mb-6 flex items-center gap-3 rounded-lg border border-border bg-muted/50 p-4 text-sm text-muted-foreground">
      <AlertTriangleIcon className="size-4 shrink-0" />
      <Trans>Billing is not configured. Please contact support to enable payment processing.</Trans>
    </div>
  );
}
