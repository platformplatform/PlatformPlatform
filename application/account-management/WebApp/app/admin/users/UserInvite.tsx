import { PlusIcon } from "lucide-react";
import { Button } from "@/ui/components/Button";

export function UserInvite() {
  return (
    <div className="flex h-24 items-center justify-between space-x-2">
      <div className="text-gray-900 text-3xl font-semibold flex gap-2 flex-col mt-10">
        <h1 className="">Users</h1>
        <p className="text-gray-500 text-sm font-normal whitespace-nowrap overflow-hidden text-ellipsis">Manage your users and permissions here.</p>
      </div>

      <Button variant="secondary" className="flex items-center gap-2 bg-black text-base font-semibold whitespace-nowrap text-white mt-2">
        <PlusIcon size={16} />
        Invite Users
      </Button>
    </div>
  );
}
