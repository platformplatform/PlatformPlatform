import { Trans } from "@lingui/react/macro";
import { trackInteraction } from "@repo/infrastructure/applicationInsights/ApplicationInsightsProvider";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { useSubscription } from "@repo/infrastructure/sync/hooks";
import { Button } from "@repo/ui/components/Button";
import { AlertTriangleIcon } from "lucide-react";

export default function PaymentFailedBanner() {
  const userInfo = useUserInfo();
  const { data: subscription } = useSubscription(userInfo?.tenantId ?? "");

  const isOwner = userInfo?.role === "Owner";

  if (!subscription?.isPaymentFailed) {
    return null;
  }

  return (
    <div className="flex h-12 items-center gap-3 border-b border-warning/50 bg-warning px-4 text-sm">
      <AlertTriangleIcon className="size-4 shrink-0 text-warning-foreground" />
      <span className="flex-1 text-warning-foreground">
        <Trans>Payment failed. Your subscription will be suspended soon.</Trans>
      </span>
      {isOwner && (
        <Button
          size="sm"
          onClick={() => {
            trackInteraction("Payment failed banner", "interaction", "Update payment method");
            globalThis.location.href = "/account/billing";
          }}
        >
          <Trans>Update payment method</Trans>
        </Button>
      )}
    </div>
  );
}
