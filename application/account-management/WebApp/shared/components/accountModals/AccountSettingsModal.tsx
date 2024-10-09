import { logoWrap } from "@/shared/images/cdnImages";
import { Button } from "@repo/ui/components/Button";
import { Dialog } from "@repo/ui/components/Dialog";
import { Modal } from "@repo/ui/components/Modal";
import { TextField } from "@repo/ui/components/TextField";
import { Heading, Label, Separator } from "react-aria-components";
import { Trash2, XIcon } from "lucide-react";
import React from "react";
import { t, Trans } from "@lingui/macro";

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
          <Trans>Account Settings</Trans>
        </Heading>
        <p className="text-muted-foreground text-sm">
          <Trans>Manage your account here.</Trans>
        </p>

        <div className="flex flex-col gap-4 mt-4">
          <Label>
            <Trans>Logo</Trans>
          </Label>
          <img src={logoWrap} alt={t`Logo`} className="max-h-16 max-w-64" />

          <TextField autoFocus isRequired name="name" label={t`Name`} placeholder={t`E.g., CompanyX`} />
          <TextField name="domain" label={t`Domain`} value="subdomain.platformplatform.net" isDisabled={true} />
        </div>

        <div className="flex flex-col gap-4 mt-6 mb-8">
          <h3 className="font-semibold">
            <Trans>Danger zone</Trans>
          </h3>
          <Separator />
          <div className="flex flex-wrap items-end gap-4">
            <div>
              <h4 className="text-sm font-semibold pt-2">
                <Trans>Delete Account</Trans>
              </h4>
              <p className="text-xs">
                <Trans>
                  Deleting the account and all associated data. This action cannot be undone, so please proceed with
                  caution.
                </Trans>
              </p>
            </div>
            <Button variant="destructive" onPress={onDeleteAccount}>
              <Trash2 />
              <Trans>Delete Account</Trans>
            </Button>
          </div>
        </div>

        <Separator className="-ml-20 -mr-20" />

        <div className="flex justify-end gap-4 mt-10">
          <Button onPress={closeDialog} variant="secondary">
            <Trans>Cancel</Trans>
          </Button>
          <Button onPress={saveChanges}>
            <Trans>Save changes</Trans>
          </Button>
        </div>
      </Dialog>
    </Modal>
  );
}
