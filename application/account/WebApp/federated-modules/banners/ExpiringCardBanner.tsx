import { Trans } from "@lingui/react/macro";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { buttonVariants } from "@repo/ui/components/Button";
import { Link } from "@repo/ui/components/Link";
import { AlertTriangleIcon } from "lucide-react";
import { api } from "@/shared/lib/api/client";

export default function ExpiringCardBanner() {
  const userInfo = useUserInfo();

  const { data: subscription } = api.useQuery(
    "get",
    "/api/account/subscriptions/current",
    {},
    { enabled: userInfo?.isAuthenticated && userInfo?.role === "Owner" }
  );

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
        <Trans>
          Your payment card is expired or expiring soon. Please update your payment method to avoid service
          interruption.
        </Trans>
      </span>
      <Link href="/account/subscription" className={buttonVariants({ size: "sm" })}>
        <Trans>Update payment method</Trans>
      </Link>
    </div>
  );
}
