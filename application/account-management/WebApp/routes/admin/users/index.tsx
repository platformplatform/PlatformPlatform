import { createFileRoute } from "@tanstack/react-router";
import { UserQuerying } from "./-components/UserQuerying";
import { UserTable } from "./-components/UserTable";
import { SharedSideMenu } from "@/shared/components/SharedSideMenu";
import { SortableUserProperties, SortOrder } from "@/shared/lib/api/client";
import { z } from "zod";
import { TopMenu } from "@/shared/components/topMenu";
import { Breadcrumb } from "@repo/ui/components/Breadcrumbs";
import { Button } from "@repo/ui/components/Button";
import { PlusIcon } from "lucide-react";
import { useState } from "react";
import InviteUserModal from "./-components/InviteUserModal";

const userPageSearchSchema = z.object({
  pageOffset: z.number().default(0).optional(),
  orderBy: z.nativeEnum(SortableUserProperties).default(SortableUserProperties.Name).optional(),
  sortOrder: z.nativeEnum(SortOrder).default(SortOrder.Ascending).optional()
});

export const Route = createFileRoute("/admin/users/")({
  component: UsersPage,
  validateSearch: userPageSearchSchema
});

export default function UsersPage() {
  const [isInviteModalOpen, setIsInviteModalOpen] = useState(false);

  return (
    <div className="flex gap-4 w-full h-full">
      <SharedSideMenu />
      <div className="flex flex-col gap-4 py-3 px-4 w-full">
        <TopMenu>
          <Breadcrumb href="/admin/users">Users</Breadcrumb>
          <Breadcrumb>All Users</Breadcrumb>
        </TopMenu>
        <div className="flex 20 w-full items-center justify-between space-x-2 sm:mt-4 mb-4">
          <div className="text-foreground text-3xl font-semibold flex gap-2 flex-col mt-3">
            <h1>Users</h1>
            <p className="text-muted-foreground text-sm font-normal">Manage your users and permissions here.</p>
          </div>
          <Button variant="primary" onPress={() => setIsInviteModalOpen(true)}>
            <PlusIcon className="w-4 h-4" />
            Invite Users
          </Button>
        </div>
        <UserQuerying />
        <UserTable />
      </div>
      <InviteUserModal isOpen={isInviteModalOpen} onOpenChange={setIsInviteModalOpen} />
    </div>
  );
}
