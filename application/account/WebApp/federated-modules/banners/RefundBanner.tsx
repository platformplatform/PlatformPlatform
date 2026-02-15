import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { Button } from "@repo/ui/components/Button";
import { InfoIcon, XIcon } from "lucide-react";
import { api, BannerType } from "@/shared/lib/api/client";

const thirtyDaysInMilliseconds = 30 * 24 * 60 * 60 * 1000;

export default function RefundBanner() {
  const userInfo = useUserInfo();

  const { data: subscription } = api.useQuery(
    "get",
    "/api/account/subscriptions/current",
    {},
    { enabled: userInfo?.isAuthenticated && userInfo?.role === "Owner" }
  );

  const dismissMutation = api.useMutation("post", "/api/account/subscriptions/dismiss-banner");

  const isOwner = userInfo?.role === "Owner";
  const refundedAt = subscription?.refundedAt;
  const isRecentRefund = refundedAt && Date.now() - new Date(refundedAt).getTime() < thirtyDaysInMilliseconds;

  if (!isRecentRefund || !isOwner) {
    return null;
  }

  const handleDismiss = () => {
    dismissMutation.mutate({ body: { bannerType: BannerType.Refund } });
  };

  return (
    <div className="flex h-12 items-center gap-3 border-warning/50 border-b bg-warning px-4 text-sm">
      <InfoIcon className="size-4 shrink-0 text-warning-foreground" />
      <span className="flex-1 text-warning-foreground">
        <Trans>A refund has been processed for your subscription. This is for your information only.</Trans>
      </span>
      <Button variant="ghost" size="sm" onClick={handleDismiss} aria-label={t`Dismiss`}>
        <XIcon className="size-4 text-warning-foreground" />
      </Button>
    </div>
  );
}
