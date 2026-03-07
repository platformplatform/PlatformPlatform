import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { tenantCollection } from "@repo/infrastructure/sync/collections";
import { useTenant, useUser } from "@repo/infrastructure/sync/hooks";
import { useElectricMutation } from "@repo/infrastructure/sync/useElectricMutation";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { Button } from "@repo/ui/components/Button";
import { Form } from "@repo/ui/components/Form";
import { Separator } from "@repo/ui/components/Separator";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { mutationSubmitter } from "@repo/ui/forms/mutationSubmitter";
import { useUnsavedChangesGuard } from "@repo/ui/hooks/useUnsavedChangesGuard";
import { createFileRoute } from "@tanstack/react-router";
import { Trash2 } from "lucide-react";
import { useEffect, useState } from "react";
import { toast } from "sonner";

import { AccountFields } from "@/shared/components/AccountFields";
import { UnsavedChangesDialog } from "@/shared/components/UnsavedChangesDialog";
import { apiClient, UserRole } from "@/shared/lib/api/client";

import { AccountInfoFields } from "./-components/AccountInfoFields";
import DeleteAccountConfirmation from "./-components/DeleteAccountConfirmation";

export const Route = createFileRoute("/account/settings/")({
  staticData: { trackingTitle: "Account settings" },
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
  const [selectedLogoFile, setSelectedLogoFile] = useState<File | null>(null);
  const [removeLogoFlag, setRemoveLogoFlag] = useState(false);
  const [isFormDirty, setIsFormDirty] = useState(false);
  const [accountName, setAccountName] = useState("");

  const { id: userId, tenantId } = import.meta.user_info_env;
  const { data: tenant } = useTenant(tenantId ?? "");
  const { data: currentUser } = useUser(userId ?? "");
  const isOwner = currentUser?.role === UserRole.Owner;

  useEffect(() => {
    if (!isFormDirty && tenant?.name !== undefined) {
      setAccountName(tenant.name);
    }
  }, [tenant?.name, isFormDirty]);

  const saveMutation = useElectricMutation({
    mutationFn: async (data: { body: { name: string } }) => {
      if (selectedLogoFile) {
        const logoFormData = new FormData();
        logoFormData.append("file", selectedLogoFile);
        const { error: logoError } = await apiClient.POST("/api/account/tenants/current/update-logo", {
          body: logoFormData as unknown as { file: string | null }
        });
        if (logoError) {
          throw logoError;
        }
      } else if (removeLogoFlag) {
        const { error: removeError } = await apiClient.DELETE("/api/account/tenants/current/remove-logo");
        if (removeError) {
          throw removeError;
        }
      }

      const { error } = await apiClient.PUT("/api/account/tenants/current", data);
      if (error) {
        throw error;
      }
    },
    utils: tenantCollection.utils,
    onMutate: () => {
      if (tenantId) {
        tenantCollection.update(tenantId, (draft) => {
          draft.name = accountName;
        });
      }
    },
    onSuccess: () => {
      setSelectedLogoFile(null);
      setRemoveLogoFlag(false);
      setIsFormDirty(false);
      toast.success(t`Account settings updated successfully`);
    }
  });

  const handleLogoFileSelect = (file: File | null) => {
    setSelectedLogoFile(file);
    setRemoveLogoFlag(false);
    setIsFormDirty(true);
  };

  const handleLogoRemove = () => {
    setRemoveLogoFlag(true);
    setIsFormDirty(true);
  };

  const { isConfirmDialogOpen, confirmLeave, cancelLeave } = useUnsavedChangesGuard({
    hasUnsavedChanges: isFormDirty && isOwner
  });

  return (
    <>
      <AppLayout
        variant="center"
        maxWidth="64rem"
        balanceWidth="16rem"
        title={t`Account settings`}
        subtitle={t`Manage your account here.`}
      >
        {tenant === undefined ? (
          <div className="mt-8 flex flex-col gap-6 md:grid md:grid-cols-[8.5rem_1fr] md:gap-8">
            <div className="flex flex-col">
              <Skeleton className="mb-2 h-5 w-24" />
              <Skeleton className="size-[8.5rem] rounded-xl" />
            </div>
            <div className="flex flex-col gap-4">
              <Skeleton className="h-16 w-full" />
              <Skeleton className="h-16 w-full" />
            </div>
          </div>
        ) : (
          <Form
            onSubmit={isOwner ? mutationSubmitter(saveMutation) : undefined}
            validationErrors={isOwner ? saveMutation.validationErrors : undefined}
            validationBehavior="aria"
            className="flex flex-col gap-4"
            onChange={() => setIsFormDirty(true)}
          >
            <AccountFields
              layout="horizontal"
              tenant={tenant}
              isPending={saveMutation.isPending}
              onLogoFileSelect={handleLogoFileSelect}
              onLogoRemove={handleLogoRemove}
              isReadOnly={!isOwner}
              tooltip={isOwner ? t`The name of your account, shown to users and in email notifications` : undefined}
              description={!isOwner ? t`Only account owners can modify the account name` : undefined}
              nameValue={accountName}
              onNameChange={setAccountName}
              onChange={() => setIsFormDirty(true)}
              infoFields={<AccountInfoFields tenant={tenant} />}
            />

            {isOwner && (
              <div className="mt-4 md:grid md:grid-cols-[8.5rem_1fr] md:gap-8">
                <div />
                <div className="flex sm:justify-end">
                  <Button type="submit" className="w-full sm:w-auto" disabled={saveMutation.isPending}>
                    {saveMutation.isPending ? <Trans>Saving...</Trans> : <Trans>Save changes</Trans>}
                  </Button>
                </div>
              </div>
            )}
          </Form>
        )}

        {isOwner && <DangerZone setIsDeleteModalOpen={setIsDeleteModalOpen} />}
      </AppLayout>

      <DeleteAccountConfirmation isOpen={isDeleteModalOpen} onOpenChange={setIsDeleteModalOpen} />

      <UnsavedChangesDialog
        isOpen={isConfirmDialogOpen}
        onConfirmLeave={confirmLeave}
        onCancel={cancelLeave}
        parentTrackingTitle="Account settings"
      />
    </>
  );
}
