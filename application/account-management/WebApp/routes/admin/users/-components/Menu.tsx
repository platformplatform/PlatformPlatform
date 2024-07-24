import { TopMenu } from "@/shared/components/topMenu";
import { Breadcrumb } from "@repo/ui/components/Breadcrumbs";

export function Menu() {
  return (
    <TopMenu>
      <Breadcrumb href="/admin/users">Users</Breadcrumb>
      <Breadcrumb>All Users</Breadcrumb>
    </TopMenu>
  );
}
