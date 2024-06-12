import Badge from "@/ui/components/Badge";
import { Tab, TabList, Tabs } from "@/ui/components/Tabs";

export function UserTabs() {
  return (
    <Tabs className="border-b-2 border-gray-200 whitespace-nowrap">
      <TabList aria-label="User Categories" className="relative items-center">
        <Tab id="allUsers" className="pb-2 gap-2">All Users <Badge variant="secondary">50</Badge></Tab>
        <Tab id="invitedUsers" className="pb-2 gap-2">Invited Users <Badge variant="secondary">50</Badge></Tab>
        <Tab id="userGroups" className="pb-2 gap-2">User Groups <Badge variant="secondary">50</Badge></Tab>
      </TabList>
    </Tabs>
  );
}
