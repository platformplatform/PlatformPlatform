import { Trans } from "@lingui/react/macro";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { InfoIcon } from "lucide-react";
import { api } from "@/shared/lib/api/client";

const thirtyDaysInMilliseconds = 30 * 24 * 60 * 60 * 1000;

export default function RefundBanner() {
  const userInfo = useUserInfo();

  const { data: subscription } = api.useQuery(
    "get",
    "/api/account/subscriptions/current",
    {},
    { enabled: userInfo?.isAuthenticated && userInfo?.role === "Owner" }
  );

  const isOwner = userInfo?.role === "Owner";
  const refundedAt = subscription?.refundedAt;
  const isRecentRefund = refundedAt && Date.now() - new Date(refundedAt).getTime() < thirtyDaysInMilliseconds;

  if (!isRecentRefund || !isOwner) {
    return null;
  }

  return (
    <div className="flex h-12 items-center gap-3 border-warning/50 border-b bg-warning px-4 text-sm">
      <InfoIcon className="size-4 shrink-0 text-warning-foreground" />
      <span className="flex-1 text-warning-foreground">
        <Trans>A refund has been processed for your subscription. This is for your information only.</Trans>
      </span>
    </div>
  );
}
