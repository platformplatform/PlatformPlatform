import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Tabs, TabsList, TabsTrigger } from "@repo/ui/components/Tabs";
import { Link } from "@tanstack/react-router";

type SubscriptionTabNavigationProps = {
  activeTab: "overview" | "plans" | "cancel";
};

export function SubscriptionTabNavigation({ activeTab }: SubscriptionTabNavigationProps) {
  return (
    <Tabs value={activeTab} className="relative z-10 mb-4 sm:mb-8">
      <TabsList aria-label={t`Subscription tabs`}>
        <TabsTrigger value="overview" nativeButton={false} render={<Link to="/account/subscription" />}>
          <Trans>Overview</Trans>
        </TabsTrigger>
        <TabsTrigger value="plans" nativeButton={false} render={<Link to="/account/subscription/plans" />}>
          <Trans>Plans</Trans>
        </TabsTrigger>
        <TabsTrigger value="cancel" nativeButton={false} render={<Link to="/account/subscription/cancel" />}>
          <Trans>Cancel subscription</Trans>
        </TabsTrigger>
      </TabsList>
    </Tabs>
  );
}
