import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { Button } from "@repo/ui/components/Button";
import { AlertTriangleIcon, XIcon } from "lucide-react";
import { api, BannerType } from "@/shared/lib/api/client";

export default function DisputeBanner() {
  const userInfo = useUserInfo();

  const { data: subscription } = api.useQuery(
    "get",
    "/api/account/subscriptions/current",
    {},
    { enabled: userInfo?.isAuthenticated && userInfo?.role === "Owner" }
  );

  const dismissMutation = api.useMutation("post", "/api/account/subscriptions/dismiss-banner");

  const isOwner = userInfo?.role === "Owner";

  if (!subscription?.disputedAt || !isOwner) {
    return null;
  }

  const handleDismiss = () => {
    dismissMutation.mutate({ body: { bannerType: BannerType.Dispute } });
  };

  return (
    <div className="flex h-12 items-center gap-3 border-warning/50 border-b bg-warning px-4 text-sm">
      <AlertTriangleIcon className="size-4 shrink-0 text-warning-foreground" />
      <span className="flex-1 text-warning-foreground">
        <span className="sm:hidden">
          <Trans>A payment dispute has been filed. Please review your payment settings.</Trans>
        </span>
        <span className="hidden sm:inline">
          <Trans>A payment dispute has been filed. Please review your payment settings or contact support.</Trans>
        </span>
      </span>
      <Button
        size="sm"
        onClick={() => {
          window.location.href = "/account/subscription";
        }}
      >
        <Trans>Review payment settings</Trans>
      </Button>
      <Button variant="ghost" size="sm" onClick={handleDismiss} aria-label={t`Dismiss`}>
        <XIcon className="size-4 text-warning-foreground" />
      </Button>
    </div>
  );
}
