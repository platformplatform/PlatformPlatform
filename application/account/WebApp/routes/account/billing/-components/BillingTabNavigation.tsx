import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Tabs, TabsList, TabsTrigger } from "@repo/ui/components/Tabs";
import { Link } from "@tanstack/react-router";

type BillingTabNavigationProps = {
  activeTab: "billing" | "subscription";
};

export function BillingTabNavigation({ activeTab }: BillingTabNavigationProps) {
  return (
    <Tabs value={activeTab} className="relative z-10 mb-4 sm:mb-8">
      <TabsList aria-label={t`Billing tabs`}>
        <TabsTrigger value="billing" nativeButton={false} render={<Link to="/account/billing" />}>
          <Trans>Billing</Trans>
        </TabsTrigger>
        <TabsTrigger value="subscription" nativeButton={false} render={<Link to="/account/billing/subscription" />}>
          <Trans>Subscription</Trans>
        </TabsTrigger>
      </TabsList>
    </Tabs>
  );
}
