import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { Badge } from "@repo/ui/components/Badge";
import { Button } from "@repo/ui/components/Button";
import { Form } from "@repo/ui/components/Form";
import { Separator } from "@repo/ui/components/Separator";
import { mutationSubmitter } from "@repo/ui/forms/mutationSubmitter";
import { useFormatDate } from "@repo/ui/hooks/useSmartDate";
import { useUnsavedChangesGuard } from "@repo/ui/hooks/useUnsavedChangesGuard";
import type { FileUploadMutation } from "@repo/ui/types/FileUpload";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { createFileRoute } from "@tanstack/react-router";
import { Trash2 } from "lucide-react";
import { useState } from "react";
import { toast } from "sonner";
import { AccountFields } from "@/shared/components/AccountFields";
import { UnsavedChangesDialog } from "@/shared/components/UnsavedChangesDialog";
import { api, type Schemas, UserRole } from "@/shared/lib/api/client";
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

function AccountInfoFields({ tenant }: { tenant: Schemas["TenantResponse"] | undefined }) {
  const formatDate = useFormatDate();

  return (
    <div className="grid grid-cols-1 gap-3 text-sm sm:flex sm:justify-between md:grid md:grid-cols-1 md:gap-3 lg:flex lg:justify-between">
      <div className="flex justify-between sm:flex-col sm:gap-1 md:flex-row md:justify-between md:gap-0 lg:flex-col lg:gap-1">
        <span className="text-muted-foreground">
          <Trans>Account ID</Trans>
        </span>
        <span className="font-mono">{tenant?.id}</span>
      </div>
      <div className="flex justify-between sm:flex-col sm:gap-1 md:flex-row md:justify-between md:gap-0 lg:flex-col lg:gap-1">
        <span className="text-muted-foreground">
          <Trans>Created</Trans>
        </span>
        <span>{formatDate(tenant?.createdAt)}</span>
      </div>
      <div className="flex justify-between sm:flex-col sm:gap-1 md:flex-row md:justify-between md:gap-0 lg:flex-col lg:gap-1">
        <span className="text-muted-foreground">
          <Trans>Status</Trans>
        </span>
        <div className="flex items-center gap-2">
          <Badge variant="secondary" className="bg-success text-success-foreground">
            <Trans>Active</Trans>
          </Badge>
        </div>
      </div>
    </div>
  );
}

export function AccountSettings() {
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [selectedLogoFile, setSelectedLogoFile] = useState<File | null>(null);
  const [removeLogoFlag, setRemoveLogoFlag] = useState(false);
  const [isFormDirty, setIsFormDirty] = useState(false);
  const queryClient = useQueryClient();

  const { data: tenant, isLoading: tenantLoading } = api.useQuery("get", "/api/account/tenants/current");
  const { data: currentUser, isLoading: userLoading } = api.useQuery("get", "/api/account/users/me");
  const updateCurrentTenantMutation = api.useMutation("put", "/api/account/tenants/current");
  const updateTenantLogoMutation = api.useMutation("post", "/api/account/tenants/current/update-logo");
  const removeTenantLogoMutation = api.useMutation("delete", "/api/account/tenants/current/remove-logo");

  const isOwner = currentUser?.role === UserRole.Owner;

  const saveMutation = useMutation<
    void,
    Schemas["HttpValidationProblemDetails"],
    { body: Schemas["UpdateCurrentTenantCommand"] }
  >({
    mutationFn: async (data) => {
      if (selectedLogoFile) {
        const formData = new FormData();
        formData.append("file", selectedLogoFile);
        await (updateTenantLogoMutation as unknown as FileUploadMutation).mutateAsync({ body: formData });
      } else if (removeLogoFlag) {
        await removeTenantLogoMutation.mutateAsync({});
      }

      await updateCurrentTenantMutation.mutateAsync(data);
      await queryClient.invalidateQueries();
      window.dispatchEvent(new CustomEvent("tenant-updated"));
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

  if (tenantLoading || userLoading) {
    return null;
  }

  return (
    <>
      <AppLayout
        variant="center"
        maxWidth="64rem"
        balanceWidth="16rem"
        title={t`Account settings`}
        subtitle={t`Manage your account here.`}
      >
        <Form
          onSubmit={isOwner ? mutationSubmitter(saveMutation) : undefined}
          validationErrors={isOwner ? saveMutation.error?.errors : undefined}
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

        {isOwner && <DangerZone setIsDeleteModalOpen={setIsDeleteModalOpen} />}
      </AppLayout>

      <DeleteAccountConfirmation isOpen={isDeleteModalOpen} onOpenChange={setIsDeleteModalOpen} />

      <UnsavedChangesDialog isOpen={isConfirmDialogOpen} onConfirmLeave={confirmLeave} onCancel={cancelLeave} />
    </>
  );
}
