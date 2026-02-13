import { Trans } from "@lingui/react/macro";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { Button } from "@repo/ui/components/Button";
import { AlertTriangleIcon } from "lucide-react";
import { api } from "@/shared/lib/api/client";

export default function DisputeBanner() {
  const userInfo = useUserInfo();

  const { data: subscription } = api.useQuery(
    "get",
    "/api/account/subscriptions/current",
    {},
    { enabled: userInfo?.isAuthenticated && userInfo?.role === "Owner" }
  );

  const isOwner = userInfo?.role === "Owner";

  if (!subscription?.disputedAt || !isOwner) {
    return null;
  }

  return (
    <div className="flex h-12 items-center gap-3 border-warning/50 border-b bg-warning px-4 text-sm">
      <AlertTriangleIcon className="size-4 shrink-0 text-warning-foreground" />
      <span className="flex-1 text-warning-foreground">
        <Trans>A payment dispute has been filed. Please review your payment settings or contact support.</Trans>
      </span>
      <Button size="sm" onClick={() => (window.location.href = "/account/subscription")}>
        <Trans>Review payment settings</Trans>
      </Button>
    </div>
  );
}
