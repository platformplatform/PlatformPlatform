import { createFileRoute } from "@tanstack/react-router";
import { Menu } from "./-components/Menu";
import { UserTabs } from "./-components/UserTabs";
import { UserQuerying } from "./-components/UserQuerying";
import { UserTable } from "./-components/UserTable";
import { UserInvite } from "./-components/UserInvite";
import { SharedSideMenu } from "@/shared/components/SharedSideMenu";
import { SortableUserProperties, SortOrder } from "@/shared/lib/api/client";
import { z } from "zod";

const userPageSearchSchema = z
  .object({
    pageOffset: z.number().catch(0),
    orderBy: z.nativeEnum(SortableUserProperties).catch(SortableUserProperties.Name),
    sortOrder: z.nativeEnum(SortOrder).catch(SortOrder.Ascending)
  })
  .catch({ pageOffset: 0, orderBy: SortableUserProperties.Name, sortOrder: SortOrder.Ascending });

export const Route = createFileRoute("/admin/users/")({
  component: UsersPage,
  validateSearch: userPageSearchSchema
});

export default function UsersPage() {
  return (
    <div className="flex gap-4 w-full h-full">
      <SharedSideMenu />
      <div className="flex flex-col gap-4 px-2 sm:px-4 py-2 md:py-4 w-full">
        <Menu />
        <UserInvite />
        <UserTabs />
        <UserQuerying />
        <UserTable />
      </div>
    </div>
  );
}
