import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { Button } from "@repo/ui/components/Button";
import { AlertTriangleIcon } from "lucide-react";
import { useEffect } from "react";
import { api, TenantState } from "@/shared/lib/api/client";

const BANNER_HEIGHT = 44;

export default function PastDueBanner() {
  const userInfo = useUserInfo();

  const { data: tenant } = api.useQuery("get", "/api/account/tenants/current");

  const billingPortalMutation = api.useMutation("post", "/api/account/subscriptions/billing-portal", {
    onSuccess: (data) => {
      if (data.portalUrl) {
        window.location.href = data.portalUrl;
      }
    }
  });

  const isPastDue = tenant?.state === TenantState.PastDue;
  const isOwner = userInfo?.role === "Owner";

  useEffect(() => {
    if (isPastDue) {
      document.documentElement.style.setProperty("--past-due-banner-height", `${BANNER_HEIGHT}px`);
    }
    return () => {
      document.documentElement.style.setProperty("--past-due-banner-height", "0px");
    };
  }, [isPastDue]);

  if (!isPastDue) {
    return null;
  }

  return (
    <>
      <div className="fixed top-0 right-0 left-0 z-[48] flex h-11 items-center gap-3 border-destructive/20 border-b bg-destructive/5 px-4 text-sm">
        <AlertTriangleIcon className="size-4 shrink-0 text-destructive" />
        <span className="flex-1 text-destructive">
          <Trans>Payment failed. Your subscription will be suspended soon.</Trans>
        </span>
        {isOwner && (
          <Button
            variant="outline"
            size="sm"
            onClick={() =>
              billingPortalMutation.mutate({
                body: { returnUrl: window.location.href }
              })
            }
            disabled={billingPortalMutation.isPending}
          >
            {billingPortalMutation.isPending ? t`Loading...` : t`Update payment method`}
          </Button>
        )}
      </div>
      <div style={{ height: BANNER_HEIGHT }} aria-hidden="true" />
    </>
  );
}
