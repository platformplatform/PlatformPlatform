import { Trans } from "@lingui/react/macro";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@repo/ui/components/Tabs";
import { BellIcon, CreditCardIcon, UserIcon } from "lucide-react";

export function TabsPreview() {
  return (
    <div className="flex flex-col gap-6">
      <Tabs defaultValue="profile">
        <TabsList>
          <TabsTrigger value="profile">
            <UserIcon />
            <Trans>Profile</Trans>
          </TabsTrigger>
          <TabsTrigger value="notifications">
            <BellIcon />
            <Trans>Notifications</Trans>
          </TabsTrigger>
          <TabsTrigger value="billing">
            <CreditCardIcon />
            <Trans>Billing</Trans>
          </TabsTrigger>
        </TabsList>
        <TabsContent value="profile">
          <p className="text-sm text-muted-foreground">
            <Trans>Update your personal information and profile photo.</Trans>
          </p>
        </TabsContent>
        <TabsContent value="notifications">
          <p className="text-sm text-muted-foreground">
            <Trans>Choose which notifications you receive and how.</Trans>
          </p>
        </TabsContent>
        <TabsContent value="billing">
          <p className="text-sm text-muted-foreground">
            <Trans>View invoices and manage your payment method.</Trans>
          </p>
        </TabsContent>
      </Tabs>
    </div>
  );
}
