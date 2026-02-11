import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { Button } from "@repo/ui/components/Button";
import { AlertTriangleIcon } from "lucide-react";
import { api, TenantState } from "@/shared/lib/api/client";

export default function PastDueBanner() {
  const userInfo = useUserInfo();

  const { data: tenant } = api.useQuery(
    "get",
    "/api/account/tenants/current",
    {},
    { enabled: userInfo?.isAuthenticated }
  );

  const billingPortalMutation = api.useMutation("post", "/api/account/subscriptions/billing-portal", {
    onSuccess: (data) => {
      if (data.portalUrl) {
        window.location.href = data.portalUrl;
      }
    }
  });

  const isPastDue = tenant?.state === TenantState.PastDue;
  const isOwner = userInfo?.role === "Owner";

  if (!isPastDue) {
    return null;
  }

  return (
    <div className="flex h-12 items-center gap-3 border-warning/50 border-b bg-warning px-4 text-sm">
      <AlertTriangleIcon className="size-4 shrink-0 text-warning-foreground" />
      <span className="flex-1 text-warning-foreground">
        <Trans>Payment failed. Your subscription will be suspended soon.</Trans>
      </span>
      {isOwner && (
        <Button
          variant="default"
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
  );
}
