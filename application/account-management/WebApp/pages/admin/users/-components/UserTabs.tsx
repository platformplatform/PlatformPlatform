import { Badge } from "@repo/ui/components/Badge";
import { Tab, TabList, Tabs } from "@repo/ui/components/Tabs";

export function UserTabs() {
  return (
    <Tabs>
      <TabList aria-label="User Categories">
        <Tab id="allUsers" href="/admin/users">
          All Users <Badge variant="secondary">50</Badge>
        </Tab>
        <Tab id="invitedUsers" href="/admin/users">
          Invited Users <Badge variant="secondary">50</Badge>
        </Tab>
        <Tab id="userGroups" href="/admin/users">
          User Groups <Badge variant="secondary">50</Badge>
        </Tab>
      </TabList>
    </Tabs>
  );
}
