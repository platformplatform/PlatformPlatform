import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { Button } from "@repo/ui/components/Button";
import { AlertTriangleIcon } from "lucide-react";
import { useState } from "react";
import { api } from "@/shared/lib/api/client";

export default function DisputeBanner() {
  const userInfo = useUserInfo();
  const [isRedirecting, setIsRedirecting] = useState(false);

  const { data: subscription } = api.useQuery(
    "get",
    "/api/account/subscriptions/current",
    {},
    { enabled: userInfo?.isAuthenticated && userInfo?.role === "Owner" }
  );

  const billingPortalMutation = api.useMutation("post", "/api/account/subscriptions/billing-portal", {
    onSuccess: (data) => {
      if (data.portalUrl) {
        setIsRedirecting(true);
        window.location.href = data.portalUrl;
      }
    }
  });

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
      <Button
        variant="default"
        size="sm"
        onClick={() =>
          billingPortalMutation.mutate({
            body: { returnUrl: window.location.href }
          })
        }
        disabled={billingPortalMutation.isPending || isRedirecting}
      >
        {billingPortalMutation.isPending || isRedirecting ? t`Loading...` : t`Review payment settings`}
      </Button>
    </div>
  );
}
