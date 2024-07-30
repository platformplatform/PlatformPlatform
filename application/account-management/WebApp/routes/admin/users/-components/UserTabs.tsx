import { useApi } from "@/shared/lib/api/client";
import { Badge } from "@repo/ui/components/Badge";
import { Tab, TabList, Tabs } from "@repo/ui/components/Tabs";

export function UserTabs() {
  const { data } = useApi("/api/account-management/users", {
    params: {
      query: {
        PageSize: 1
      }
    }
  });

  return (
    <Tabs>
      <TabList aria-label="User Categories">
        <Tab id="allUsers" href="/admin/users">
          All Users <Badge variant="secondary">{data?.totalCount}</Badge>
        </Tab>
        <Tab id="invitedUsers" href="/admin/users">
          Invited Users <Badge variant="secondary">2</Badge>
        </Tab>
        <Tab id="userGroups" href="/admin/users">
          User Groups <Badge variant="secondary">5</Badge>
        </Tab>
      </TabList>
    </Tabs>
  );
}
