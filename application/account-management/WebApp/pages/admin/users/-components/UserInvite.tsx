import { PlusIcon } from "lucide-react";
import { Button } from "@repo/ui/components/Button";

export function UserInvite() {
  return (
    <div className="flex h-24 items-center justify-between space-x-2 mt-4 mb-4">
      <div className="text-foreground text-3xl font-semibold flex gap-2 flex-col mt-6">
        <h1 className="">Users</h1>
        <p className="text-muted-foreground text-sm font-normal whitespace-nowrap overflow-hidden text-ellipsis">
          Manage your users and permissions here.
        </p>
      </div>

      <Button variant="primary" className="flex items-center gap-2 whitespace-nowrap mt-2">
        <PlusIcon size={16} />
        Invite Users
      </Button>
    </div>
  );
}
