import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { Button } from "@repo/ui/components/Button";
import { Form } from "@repo/ui/components/Form";
import { Separator } from "@repo/ui/components/Separator";
import { mutationSubmitter } from "@repo/ui/forms/mutationSubmitter";
import { useUnsavedChangesGuard } from "@repo/ui/hooks/useUnsavedChangesGuard";
import type { FileUploadMutation } from "@repo/ui/types/FileUpload";
import { useQueryClient } from "@tanstack/react-query";
import { createFileRoute } from "@tanstack/react-router";
import { Trash2 } from "lucide-react";
import { useEffect, useState } from "react";
import { toast } from "sonner";
import { AccountFields } from "@/shared/components/AccountFields";
import { UnsavedChangesDialog } from "@/shared/components/UnsavedChangesDialog";
import { api, UserRole } from "@/shared/lib/api/client";
import DeleteAccountConfirmation from "./-components/DeleteAccountConfirmation";

export const Route = createFileRoute("/account/settings/")({
  component: AccountSettings
});

// Danger zone component
function DangerZone({ setIsDeleteModalOpen }: { setIsDeleteModalOpen: (open: boolean) => void }) {
  return (
    <div className="mt-12 flex flex-col gap-4">
      <h3>
        <Trans>Danger zone</Trans>
      </h3>
      <Separator />
      <div className="flex flex-col gap-4">
        <p className="text-sm">
          <Trans>Delete your account and all data. This action is irreversible—proceed with caution.</Trans>
        </p>

        <Button variant="destructive" onClick={() => setIsDeleteModalOpen(true)} className="w-fit max-sm:w-full">
          <Trash2 />
          <Trans>Delete account</Trans>
        </Button>
      </div>
    </div>
  );
}

export function AccountSettings() {
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [isFormDirty, setIsFormDirty] = useState(false);
  const queryClient = useQueryClient();

  const {
    data: tenant,
    isLoading: tenantLoading,
    refetch: refetchTenant
  } = api.useQuery("get", "/api/account/tenants/current");
  const { data: currentUser, isLoading: userLoading } = api.useQuery("get", "/api/account/users/me");
  const updateCurrentTenantMutation = api.useMutation("put", "/api/account/tenants/current", {
    onSuccess: () => {
      setIsFormDirty(false);
      toast.success(t`Account name updated successfully`);
      refetchTenant();
    }
  });
  const updateTenantLogoMutation = api.useMutation("post", "/api/account/tenants/current/update-logo");
  const removeTenantLogoMutation = api.useMutation("delete", "/api/account/tenants/current/remove-logo");

  const isOwner = currentUser?.role === UserRole.Owner;

  const handleLogoFileSelect = async (file: File | null) => {
    if (file) {
      const formData = new FormData();
      formData.append("file", file);
      await (updateTenantLogoMutation as unknown as FileUploadMutation).mutateAsync({ body: formData });
      await queryClient.invalidateQueries();
      refetchTenant();
      toast.success(t`Logo uploaded successfully`);
    }
  };

  const handleLogoRemove = async () => {
    await removeTenantLogoMutation.mutateAsync({});
    await queryClient.invalidateQueries();
    refetchTenant();
    toast.success(t`Logo removed successfully`);
  };

  const { isConfirmDialogOpen, confirmLeave, cancelLeave } = useUnsavedChangesGuard({
    hasUnsavedChanges: isFormDirty && isOwner
  });

  // Dispatch event to notify components about tenant updates
  useEffect(() => {
    if (
      updateCurrentTenantMutation.isSuccess ||
      updateTenantLogoMutation.isSuccess ||
      removeTenantLogoMutation.isSuccess
    ) {
      window.dispatchEvent(new CustomEvent("tenant-updated"));
    }
  }, [updateCurrentTenantMutation.isSuccess, updateTenantLogoMutation.isSuccess, removeTenantLogoMutation.isSuccess]);

  if (tenantLoading || userLoading) {
    return null;
  }

  return (
    <>
      <AppLayout variant="center" title={t`Account settings`} subtitle={t`Manage your account here.`}>
        <Form
          onSubmit={isOwner ? mutationSubmitter(updateCurrentTenantMutation) : undefined}
          validationErrors={isOwner ? updateCurrentTenantMutation.error?.errors : undefined}
          validationBehavior="aria"
          className="flex flex-col gap-4"
          onChange={() => setIsFormDirty(true)}
        >
          <h3>
            <Trans>Account information</Trans>
          </h3>
          <Separator />

          <AccountFields
            tenant={tenant}
            isPending={updateCurrentTenantMutation.isPending}
            onLogoFileSelect={handleLogoFileSelect}
            onLogoRemove={handleLogoRemove}
            isReadOnly={!isOwner}
            tooltip={isOwner ? t`The name of your account, shown to users and in email notifications` : undefined}
            description={!isOwner ? t`Only account owners can modify the account name` : undefined}
            onChange={() => setIsFormDirty(true)}
          />

          {isOwner && (
            <Button type="submit" className="mt-4 w-fit max-sm:w-full" disabled={updateCurrentTenantMutation.isPending}>
              {updateCurrentTenantMutation.isPending ? <Trans>Saving...</Trans> : <Trans>Save changes</Trans>}
            </Button>
          )}
        </Form>

        {isOwner && <DangerZone setIsDeleteModalOpen={setIsDeleteModalOpen} />}
      </AppLayout>

      <DeleteAccountConfirmation isOpen={isDeleteModalOpen} onOpenChange={setIsDeleteModalOpen} />

      <UnsavedChangesDialog isOpen={isConfirmDialogOpen} onConfirmLeave={confirmLeave} onCancel={cancelLeave} />
    </>
  );
}
