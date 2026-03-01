import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Tabs, TabsList, TabsTrigger } from "@repo/ui/components/Tabs";
import { Link } from "@tanstack/react-router";

type UserTabNavigationProps = {
  activeTab: "all-users" | "recycle-bin";
};

export function UserTabNavigation({ activeTab }: UserTabNavigationProps) {
  return (
    <Tabs value={activeTab} className="relative z-10 mb-4 sm:mb-8">
      <TabsList aria-label={t`User tabs`}>
        <TabsTrigger value="all-users" nativeButton={false} render={<Link to="/account/users" />}>
          <Trans>All users</Trans>
        </TabsTrigger>
        <TabsTrigger value="recycle-bin" nativeButton={false} render={<Link to="/account/users/recycle-bin" />}>
          <Trans>Recycle bin</Trans>
        </TabsTrigger>
      </TabsList>
    </Tabs>
  );
}
