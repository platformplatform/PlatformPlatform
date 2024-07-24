import { AlertDialog } from "@repo/ui/components/AlertDialog";
import { Button } from "@repo/ui/components/Button";
import { Input } from "@repo/ui/components/Input";
import { Modal } from "@repo/ui/components/Modal";
import { Trash2, XIcon } from "lucide-react";

type AccountSettingsModal = {
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
  onDeleteAccount: () => void;
};

export function AccountSettingsModal({ isOpen, onOpenChange, onDeleteAccount }: Readonly<AccountSettingsModal>) {
  const logoWrap = "https://platformplatformgithub.blob.core.windows.net/logo-wrap.svg?url";

  return (
    <Modal isOpen={isOpen} onOpenChange={onOpenChange} isDismissable>
      <AlertDialog actionLabel="Save changes" title="" onAction={() => onOpenChange(false)}>
        <Button onPress={() => onOpenChange(false)} className="absolute top-0 right-0 m-3" variant="ghost" size="sm">
          <XIcon className="w-4 h-4" />
        </Button>
        <div className="flex flex-col text-gray-900 text-xl font-semibold pb-8">
          <div className="pb-4">
            <h1>Account Settings</h1>
            <h2 className="text-slate-600 text-sm font-normal">Manage your account here</h2>
          </div>
          <div className="w-full flex-col flex pb-8 gap-3 text-slate-700 text-sm font-medium">
            <div>
              <label>Logo</label>
              <img src={logoWrap} alt="Logo" className="my-4" />
            </div>
            <div className="flex flex-col gap-1">
              <label>Name</label>
              <Input placeholder="Name" />
            </div>
            <div className="flex flex-col gap-1">
              <label>Domain</label>
              <Input value="subdomain.platformplatform.net" isDisabled={true} />
            </div>
          </div>
          <h3 className="text-base pb-2">Danger zone</h3>
          <div className="w-full border-b border-border" aria-hidden />
          <div className="flex flex-wrap items-end pb-12 gap-4 border-b-2 border-gray-200">
            <div className="flex-1 text-black">
              <h4 className="text-sm pt-2">Delete Account</h4>
              <p className="text-xs font-normal">
                Deleting the account and all associated data.
                <br />
                This action is not reversible, so please continue with caution.
              </p>
            </div>
            <Button variant="destructive" onPress={onDeleteAccount}>
              <Trash2 />
              Delete Account
            </Button>
          </div>
        </div>
      </AlertDialog>
    </Modal>
  );
}

export default AccountSettingsModal;
