import { SharedSideMenu } from "@/shared/components/SharedSideMenu";
import { TopMenu } from "@/shared/components/topMenu";
import logoWrap from "@/shared/images/logo-wrap.svg";
import { api } from "@/shared/lib/api/client";
import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { Breadcrumb } from "@repo/ui/components/Breadcrumbs";
import { Button } from "@repo/ui/components/Button";
import { Form } from "@repo/ui/components/Form";
import { FormErrorMessage } from "@repo/ui/components/FormErrorMessage";
import { TextField } from "@repo/ui/components/TextField";
import { mutationSubmitter } from "@repo/ui/forms/mutationSubmitter";
import { createFileRoute } from "@tanstack/react-router";
import { Trash2 } from "lucide-react";
import { useState } from "react";
import { Separator } from "react-aria-components";
import DeleteAccountConfirmation from "./-components/DeleteAccountConfirmation";

export const Route = createFileRoute("/admin/account/")({
  component: AccountSettings
});

export function AccountSettings() {
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const { data: tenant, isLoading } = api.useQuery("get", "/api/account-management/tenants/current");
  const updateCurrentTenantMutation = api.useMutation("put", "/api/account-management/tenants/current");

  if (isLoading) {
    return null;
  }

  return (
    <>
      <SharedSideMenu ariaLabel={t`Toggle collapsed menu`} />
      <AppLayout
        variant="center"
        topMenu={
          <TopMenu>
            <Breadcrumb href="/admin/account">
              <Trans>Account</Trans>
            </Breadcrumb>
            <Breadcrumb>
              <Trans>Settings</Trans>
            </Breadcrumb>
          </TopMenu>
        }
      >
        <h1>
          <Trans>Account settings</Trans>
        </h1>
        <p>
          <Trans>Manage your account here.</Trans>
        </p>

        <Form
          onSubmit={mutationSubmitter(updateCurrentTenantMutation)}
          validationErrors={updateCurrentTenantMutation.error?.errors}
          validationBehavior="aria"
          className="flex flex-col gap-4"
        >
          <h2>
            <Trans>Account information</Trans>
          </h2>
          <Separator />

          <Trans>Logo</Trans>

          <img src={logoWrap} alt={t`Logo`} className="max-h-16 max-w-64" />
          <TextField
            autoFocus={true}
            isRequired={true}
            name="name"
            defaultValue={tenant?.name ?? ""}
            isDisabled={updateCurrentTenantMutation.isPending}
            label={t`Account name`}
            validationBehavior="aria"
          />
          <FormErrorMessage error={updateCurrentTenantMutation.error} />
          <Button type="submit" className="mt-4" isDisabled={updateCurrentTenantMutation.isPending}>
            {updateCurrentTenantMutation.isPending ? <Trans>Saving...</Trans> : <Trans>Save changes</Trans>}
          </Button>
        </Form>

        <div className="mt-6 flex flex-col gap-4">
          <h2>
            <Trans>Danger zone</Trans>
          </h2>
          <Separator />
          <div className="flex flex-col gap-4">
            <p>
              <Trans>Delete your account and all data. This action is irreversible—proceed with caution.</Trans>
            </p>

            <Button variant="destructive" onPress={() => setIsDeleteModalOpen(true)} className="w-fit">
              <Trash2 />
              <Trans>Delete account</Trans>
            </Button>
          </div>
        </div>
      </AppLayout>

      <DeleteAccountConfirmation isOpen={isDeleteModalOpen} onOpenChange={setIsDeleteModalOpen} />
    </>
  );
}
