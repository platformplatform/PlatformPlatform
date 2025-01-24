import logoWrap from "@/shared/images/logo-wrap.svg";
import { Button } from "@repo/ui/components/Button";
import { TextField } from "@repo/ui/components/TextField";
import { Label, Separator } from "react-aria-components";
import { Trash2 } from "lucide-react";
import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { createFileRoute } from "@tanstack/react-router";
import { SharedSideMenu } from "@/shared/components/SharedSideMenu";
import { TopMenu } from "@/shared/components/topMenu";
import { Breadcrumb } from "@repo/ui/components/Breadcrumbs";
import { useState } from "react";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import DeleteAccountConfirmation from "./-components/DeleteAccountConfirmation";

export const Route = createFileRoute("/admin/account/")({
  component: AccountSettings
});

function AccountSettings() {
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const userInfo = useUserInfo();
  if (userInfo === null) return null;

  const saveChanges = () => {
    console.log("Saving changes");
  };

  const handleDeleteAccount = () => {
    setIsDeleteModalOpen(true);
  };

  return (
    <>
      <div className="flex gap-4 w-full h-full">
        <SharedSideMenu />
        <div className="flex flex-col gap-4 py-3 px-4 w-full">
          <TopMenu>
            <Breadcrumb href="/admin/account">
              <Trans>Account</Trans>
            </Breadcrumb>
            <Breadcrumb>
              <Trans>Settings</Trans>
            </Breadcrumb>
          </TopMenu>
          <div className="flex 20 w-full items-center justify-between space-x-2 sm:mt-4 mb-4">
            <div className="text-foreground text-3xl font-semibold flex gap-2 flex-col mt-3">
              <h1>
                <Trans>Account Settings</Trans>
              </h1>
              <p className="text-muted-foreground text-sm font-normal">
                <Trans>Manage your account here.</Trans>
              </p>
            </div>
          </div>

          <div className="flex flex-col gap-4">
            <Label>
              <Trans>Logo</Trans>
            </Label>
            <img src={logoWrap} alt={t`Logo`} className="max-h-16 max-w-64" />

            <div className="w-full md:max-w-md">
              <TextField autoFocus isRequired name="name" label={t`Name`} placeholder={`E.g. ${userInfo.tenantId}`} />
            </div>
            <div className="w-full md:max-w-md">
              <TextField
                name="domain"
                label={t`Domain`}
                value={`${userInfo.tenantId}.platformplatform.net`}
                isDisabled={true}
              />
            </div>
          </div>

          <div className="flex gap-4">
            <Button onPress={saveChanges}>
              <Trans>Save changes</Trans>
            </Button>
          </div>

          <div className="flex flex-col gap-4 mt-6">
            <h3 className="font-semibold">
              <Trans>Danger zone</Trans>
            </h3>
            <Separator />
            <div className="flex flex-col gap-4">
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
              <Button variant="destructive" onPress={handleDeleteAccount} className="w-fit">
                <Trash2 />
                <Trans>Delete Account</Trans>
              </Button>
            </div>
          </div>

          <Separator className="my-8" />
        </div>
      </div>

      <DeleteAccountConfirmation isOpen={isDeleteModalOpen} onOpenChange={setIsDeleteModalOpen} />
    </>
  );
}
