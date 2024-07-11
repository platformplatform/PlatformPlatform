import { AlertDialog } from "@repo/ui/components/AlertDialog";
import { Button } from "@repo/ui/components/Button";
import { Input } from "@repo/ui/components/Field";
import { Modal } from "@repo/ui/components/Modal";
import { TrashIcon, XIcon } from "lucide-react";


type AccountModalProps = {
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
  onDeleteAccount: () => void;
};


export function AccountModal( { isOpen, onOpenChange, onDeleteAccount }: AccountModalProps) {
const logoWrap = "https://platformplatformgithub.blob.core.windows.net/logo-wrap.svg?url";

return (
<>
  <Modal
  isOpen={isOpen}
  onOpenChange={onOpenChange}
  isDismissable
  className="w-full max-w-6xl"
>
  <AlertDialog variant="info" actionLabel="Save changes" title="" onAction={() => onOpenChange(false)}>
    <Button onPress={() => onOpenChange(false)} className="absolute top-0 right-0 p-2" variant="icon">
      <XIcon name="cross" />
    </Button>
    <div className="flex flex-col text-gray-900 text-xl font-semibold pb-8">
      <div className="border-b-2 border-gray-200">
        <div className="pb-4">
          <h1>Account Settings</h1>
          <h2 className=" text-slate-600 text-sm font-normal">Manage your account here</h2>
        </div>
        <div className="w-full flex-col flex pb-8 gap-3 text-slate-700 text-sm font-medium">
          <div>
            <label>logo</label>
            <img src={logoWrap} alt="Logo" className="my-4" />
          </div>
          <div className="flex flex-col gap-1">
            <label>Name</label>
            <Input placeholder="Name" />
          </div>
          <div className="flex flex-col gap-1">
            <label>Domain</label>
            <Input placeholder="companyx.platformplatform.com" />
          </div>
        </div>
        <h3 className="text-base pb-2">Danger Zone</h3>
      </div>
      <div className="flex flex-wrap items-end w-full pb-12 gap-4 border-b-2 border-gray-200">
        <div className="flex-1 text-black">
          <h4 className="text-sm pt-2">Delete Account</h4>
          <p className="text-xs font-normal">
            Deleting the account and all associated data.
            <br />
            This action is not reversible, so please continue with caution.
          </p>
        </div>
        <div className="shrink-0">
          <Button
            variant="destructive"
            onPress={ onDeleteAccount}
            className="flex items-center gap-1"
          >
            <TrashIcon />
            Delete Account
          </Button>
        </div>
      </div>
    </div>
  </AlertDialog>
</Modal>
</>
);
}

export default AccountModal;


