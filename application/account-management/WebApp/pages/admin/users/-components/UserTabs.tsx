import { Badge } from "@repo/ui/components/Badge";
import { Tab, TabList, Tabs } from "@repo/ui/components/Tabs";
import type { components } from "@/shared/lib/api/api.generated";

type UserTableProps = {
  usersData: components["schemas"]["GetUsersResponseDto"] | null;
};

export function UserTabs({ usersData }: Readonly<UserTableProps>) {
  return (
    <Tabs>
      <TabList aria-label="User Categories">
        <Tab id="allUsers" href="/admin/users">
          All Users <Badge variant="secondary">{usersData?.totalCount}</Badge>
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
