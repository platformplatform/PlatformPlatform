import { PlusIcon } from "lucide-react";
import { Button } from "@repo/ui/components/Button";

export function UserInvite() {
  return (
    <div className="flex h-24 w-full items-center justify-between space-x-2 sm:mt-4 mb-4">
      <div className="text-foreground text-3xl font-semibold flex gap-2 flex-col mt-3">
        <h1 className="">Users</h1>
        <p className="text-muted-foreground text-sm font-normal">Manage your users and permissions here.</p>
      </div>

      <Button variant="primary">
        <PlusIcon className="w-4 h-4" />
        Invite Users
      </Button>
    </div>
  );
}
