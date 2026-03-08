import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { useSubscription } from "@repo/infrastructure/sync/hooks";
import { Badge } from "@repo/ui/components/Badge";
import { useFormatDate } from "@repo/ui/hooks/useSmartDate";
import { Link } from "@tanstack/react-router";

import { SuspensionReason, TenantState } from "@/shared/lib/api/client";
import { getPlanLabelWithFree } from "@/shared/lib/api/subscriptionPlan";

interface TenantInfo {
  id: string;
  createdAt: string;
  state: string;
  suspensionReason: string | null;
}

interface AccountInfoFieldsProps {
  tenant: Readonly<TenantInfo | undefined>;
}

const isSubscriptionEnabled = import.meta.runtime_env.PUBLIC_SUBSCRIPTION_ENABLED === "true";

function getSuspensionReasonLabel(reason: string | null | undefined): string {
  switch (reason) {
    case SuspensionReason.PaymentFailed:
      return t`Payment failed`;
    case SuspensionReason.CustomerDeleted:
      return t`Customer deleted`;
    default:
      return "";
  }
}

export function AccountInfoFields({ tenant }: Readonly<AccountInfoFieldsProps>) {
  const formatDate = useFormatDate();
  const { tenantId } = import.meta.user_info_env;
  const { data: subscription } = useSubscription(tenantId ?? "");

  const isSuspended = tenant?.state === TenantState.Suspended;

  return (
    <div className="grid grid-cols-1 gap-3 text-sm sm:flex sm:justify-between md:grid md:grid-cols-1 md:gap-3 lg:flex lg:justify-between">
      <div className="flex justify-between sm:flex-col sm:gap-1 md:flex-row md:justify-between md:gap-0 lg:flex-col lg:gap-1">
        <span className="text-muted-foreground">
          <Trans>Account ID</Trans>
        </span>
        <span className="font-mono">{tenant?.id}</span>
      </div>
      <div className="flex justify-between sm:flex-col sm:gap-1 md:flex-row md:justify-between md:gap-0 lg:flex-col lg:gap-1">
        <span className="text-muted-foreground">
          <Trans>Created</Trans>
        </span>
        <span>{formatDate(tenant?.createdAt)}</span>
      </div>
      <div className="flex justify-between sm:flex-col sm:gap-1 md:flex-row md:justify-between md:gap-0 lg:flex-col lg:gap-1">
        <span className="text-muted-foreground">
          <Trans>Status</Trans>
        </span>
        <div className="flex items-center gap-2">
          {isSuspended ? (
            <>
              <Badge variant="destructive">
                <Trans>Suspended</Trans>
              </Badge>
              {tenant?.suspensionReason && (
                <span className="text-muted-foreground">{getSuspensionReasonLabel(tenant.suspensionReason)}</span>
              )}
            </>
          ) : (
            <Badge variant="secondary" className="bg-success text-success-foreground">
              <Trans>Active</Trans>
            </Badge>
          )}
        </div>
      </div>
      {isSubscriptionEnabled && (
        <div className="flex justify-between sm:flex-col sm:gap-1 md:flex-row md:justify-between md:gap-0 lg:flex-col lg:gap-1">
          <span className="text-muted-foreground">
            <Trans>Current plan</Trans>
          </span>
          <Link to="/account/billing/subscription" className="text-primary hover:underline">
            {getPlanLabelWithFree(subscription?.plan)}
          </Link>
        </div>
      )}
    </div>
  );
}
