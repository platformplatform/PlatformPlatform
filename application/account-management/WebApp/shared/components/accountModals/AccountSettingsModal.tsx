import { Button } from "@repo/ui/components/Button";
import { Dialog } from "@repo/ui/components/Dialog";
import { Modal } from "@repo/ui/components/Modal";
import { TextField } from "@repo/ui/components/TextField";
import { Heading, Label, Separator } from "react-aria-components";
import { Trash2, XIcon } from "lucide-react";
import React from "react";

type AccountSettingsModal = {
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
  onDeleteAccount: () => void;
};

export default function AccountSettingsModal({
  isOpen,
  onOpenChange,
  onDeleteAccount
}: Readonly<AccountSettingsModal>) {
  const logoWrap = "https://platformplatformgithub.blob.core.windows.net/logo-wrap.svg?url";

  const saveChanges = () => {
    console.log("Saving changes");
    closeDialog();
  };

  const closeDialog = () => {
    onOpenChange(false);
  };

  return (
    <Modal isOpen={isOpen} onOpenChange={onOpenChange} isDismissable>
      <Dialog>
        <XIcon onClick={closeDialog} className="h-10 w-10 absolute top-2 right-2 p-2 hover:bg-muted" />
        <Heading slot="title" className="text-2xl">
          Account Settings
        </Heading>
        <p className="text-muted-foreground text-sm">Manage your account here.</p>

        <div className="flex flex-col gap-4 mt-4">
          <Label>Logo</Label>
          <img src={logoWrap} alt="Logo" className="max-h-16 max-w-64" />

          <TextField autoFocus isRequired name="name" label="Name" placeholder="E.g. CompanyX" />
          <TextField name="domain" label="Domain" value="subdomain.platformplatform.net" isDisabled={true} />
        </div>

        <div className="flex flex-col gap-4 mt-6 mb-8">
          <h3 className="font-semibold">Danger zone</h3>
          <Separator />
          <div className="flex flex-wrap items-end gap-4">
            <div>
              <h4 className="text-sm font-semibold pt-2">Delete Account</h4>
              <p className="text-xs">
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

        <Separator className="-ml-20 -mr-20" />

        <div className="flex justify-end gap-4 mt-10">
          <Button onPress={closeDialog} variant="secondary">
            Cancel
          </Button>
          <Button onPress={saveChanges}>Save changes</Button>
        </div>
      </Dialog>
    </Modal>
  );
}
