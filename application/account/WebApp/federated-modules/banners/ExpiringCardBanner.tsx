import { Trans } from "@lingui/react/macro";
import { trackInteraction } from "@repo/infrastructure/applicationInsights/ApplicationInsightsProvider";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { useSubscription } from "@repo/infrastructure/sync/hooks";
import { Button } from "@repo/ui/components/Button";
import { AlertTriangleIcon } from "lucide-react";

export default function ExpiringCardBanner() {
  const userInfo = useUserInfo();
  const { data: subscription } = useSubscription(userInfo?.tenantId ?? "");

  const paymentMethod = subscription?.paymentMethod;
  const currentPeriodEnd = subscription?.currentPeriodEnd;
  const cancelAtPeriodEnd = subscription?.cancelAtPeriodEnd ?? false;
  const isOwner = userInfo?.role === "Owner";

  const isCardExpiringSoon =
    paymentMethod &&
    paymentMethod.brand !== "link" &&
    currentPeriodEnd &&
    !cancelAtPeriodEnd &&
    new Date(currentPeriodEnd) >= new Date(paymentMethod.expYear, paymentMethod.expMonth, 1);

  if (!isCardExpiringSoon || !isOwner) {
    return null;
  }

  return (
    <div className="flex h-12 items-center gap-3 border-warning/50 border-b bg-warning px-4 text-sm">
      <AlertTriangleIcon className="size-4 shrink-0 text-warning-foreground" />
      <span className="flex-1 text-warning-foreground">
        <span className="sm:hidden">
          <Trans>Your payment card is expiring soon. Update your payment method to avoid interruption.</Trans>
        </span>
        <span className="hidden sm:inline">
          <Trans>
            Your payment card is expired or expiring soon. Please update your payment method to avoid service
            interruption.
          </Trans>
        </span>
      </span>
      <Button
        size="sm"
        onClick={() => {
          trackInteraction("Expiring card banner", "interaction", "Update payment method");
          window.location.href = "/account/billing";
        }}
      >
        <Trans>Update payment method</Trans>
      </Button>
    </div>
  );
}
