import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { Button } from "@repo/ui/components/Button";
import { AlertTriangleIcon } from "lucide-react";
import { api, TenantState } from "@/shared/lib/api/client";

export function PastDueBanner() {
  const userInfo = useUserInfo();

  const { data: tenant } = api.useQuery("get", "/api/account-management/tenants/current");

  const billingPortalMutation = api.useMutation("post", "/api/account-management/subscriptions/billing-portal", {
    onSuccess: (data) => {
      if (data.portalUrl) {
        window.location.href = data.portalUrl;
      }
    }
  });

  if (tenant?.state !== TenantState.PastDue) {
    return null;
  }

  const isOwner = userInfo?.role === "Owner";

  return (
    <div className="flex items-center gap-3 border-destructive/20 border-b bg-destructive/5 px-4 py-2.5 text-sm">
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
  );
}
