import { PlusIcon } from "lucide-react";
import { Button } from "@/ui/components/Button";

export function UserInvite() {
  return (
    <div className="flex justify-between items-center">
      <div className="text-gray-900 text-3xl font-semibold flex gap-2 flex-col">
        <h1 className="">Users</h1>
        <p className="text-slate-600 text-sm font-normal">Manage your users and permissions here.</p>
      </div>

      <Button variant="secondary" className="flex gap-2 text-slate-700 text-sm font-semibold items-center">
        <PlusIcon size={16} />
        Invite Users
      </Button>
    </div>
  );
}
