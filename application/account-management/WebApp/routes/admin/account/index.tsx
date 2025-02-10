import logoWrap from "@/shared/images/logo-wrap.svg";
import { Button } from "@repo/ui/components/Button";
import { TextField } from "@repo/ui/components/TextField";
import { Label, Separator } from "react-aria-components";
import { Trash2 } from "lucide-react";
import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { createFileRoute } from "@tanstack/react-router";
import { Form } from "@repo/ui/components/Form";
import { useEffect, useState } from "react";
import { api } from "@/shared/lib/api/client";
import DeleteAccountConfirmation from "./-components/DeleteAccountConfirmation";
import { SharedSideMenu } from "@/shared/components/SharedSideMenu";
import { TopMenu } from "@/shared/components/topMenu";
import { Breadcrumb } from "@repo/ui/components/Breadcrumbs";
import { GeneralFormErrorMessage } from "@repo/ui/components/GeneralFormErrorMessage";

export const Route = createFileRoute("/admin/account/")({
  component: AccountSettings
});

export function AccountSettings() {
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const { data: tenant, isLoading, refetch } = api.useQuery("get", "/api/account-management/tenants/current");
  const updateCurrentTenantMutation = api.useMutation("put", "/api/account-management/tenants/current");

  const handleSubmit = (formData: FormData) => {
    // biome-ignore lint/suspicious/noExplicitAny: Same as we do in PlatformServerAction.ts
    updateCurrentTenantMutation.mutate({ body: Object.fromEntries(formData) as any });
  };

  useEffect(() => {
    if (updateCurrentTenantMutation.isSuccess) {
      refetch();
    }
  }, [updateCurrentTenantMutation.isSuccess, refetch]);

  if (isLoading) return null;

  return (
    <>
      <div className="flex gap-4 w-full h-full">
        <SharedSideMenu ariaLabel={t`Toggle collapsed menu`} />
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

          <Form
            action={handleSubmit}
            validationErrors={updateCurrentTenantMutation.error?.errors}
            validationBehavior="aria"
            className="flex flex-col gap-4"
          >
            <Label>
              <Trans>Logo</Trans>
            </Label>
            <img src={logoWrap} alt={t`Logo`} className="max-h-16 max-w-64" />

            <div className="w-full md:max-w-md">
              <TextField
                autoFocus
                isRequired
                name="name"
                defaultValue={tenant?.name ?? ""}
                label={t`Account name`}
                validationBehavior="aria"
              />
            </div>
            <GeneralFormErrorMessage error={updateCurrentTenantMutation.error} />
            <Button type="submit" className="mt-4">
              <Trans>Save changes</Trans>
            </Button>
          </Form>

          <div className="flex flex-col gap-4 mt-6">
            <h3 className="font-semibold">
              <Trans>Danger zone</Trans>
            </h3>
            <Separator />
            <div className="flex flex-col gap-4">
              <p className="text-sm font-normal">
                <Trans>
                  Deleting the account and all associated data. This action cannot be undone, so please proceed with
                  caution.
                </Trans>
              </p>

              <Button variant="destructive" onPress={() => setIsDeleteModalOpen(true)} className="w-fit">
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
