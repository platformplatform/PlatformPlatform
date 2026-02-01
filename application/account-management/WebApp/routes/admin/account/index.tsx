import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { BreadcrumbPage } from "@repo/ui/components/Breadcrumb";
import { Button } from "@repo/ui/components/Button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger
} from "@repo/ui/components/DropdownMenu";
import { Field, FieldLabel } from "@repo/ui/components/Field";
import { Form } from "@repo/ui/components/Form";
import { Separator } from "@repo/ui/components/Separator";
import { TenantLogo } from "@repo/ui/components/TenantLogo";
import { TextField } from "@repo/ui/components/TextField";
import { mutationSubmitter } from "@repo/ui/forms/mutationSubmitter";
import { useUnsavedChangesGuard } from "@repo/ui/hooks/useUnsavedChangesGuard";
import type { FileUploadMutation } from "@repo/ui/types/FileUpload";
import { useQueryClient } from "@tanstack/react-query";
import { createFileRoute } from "@tanstack/react-router";
import { CameraIcon, Trash2, Trash2Icon } from "lucide-react";
import type React from "react";
import { useCallback, useEffect, useRef, useState } from "react";
import { toast } from "sonner";
import FederatedSideMenu from "@/federated-modules/sideMenu/FederatedSideMenu";
import { TopMenu } from "@/shared/components/topMenu";
import { UnsavedChangesDialog } from "@/shared/components/UnsavedChangesDialog";
import { api, UserRole } from "@/shared/lib/api/client";
import DeleteAccountConfirmation from "./-components/DeleteAccountConfirmation";

export const Route = createFileRoute("/admin/account/")({
  component: AccountSettings
});

const MAX_FILE_SIZE = 1024 * 1024; // 1MB in bytes
const ALLOWED_FILE_TYPES = ["image/jpeg", "image/png", "image/gif", "image/webp", "image/svg+xml"]; // Align with backend

// Helper function for file validation
function validateLogoFile(file: File): boolean {
  if (!ALLOWED_FILE_TYPES.includes(file.type)) {
    alert(t`Please select a JPEG, PNG, GIF, WebP, or SVG image.`);
    return false;
  }

  if (file.size > MAX_FILE_SIZE) {
    alert(t`Image must be smaller than 1 MB.`);
    return false;
  }

  return true;
}

// Custom hook for managing logo state
function useLogoManagement(
  updateTenantLogoMutation: FileUploadMutation,
  removeTenantLogoMutation: { mutateAsync: (params: Record<string, never>) => Promise<unknown> },
  refetchTenant: () => void,
  queryClient: ReturnType<typeof useQueryClient>,
  logoFileInputRef: React.RefObject<HTMLInputElement | null>
) {
  const [logoPreviewUrl, setLogoPreviewUrl] = useState<string | null>(null);
  const [logoMenuOpen, setLogoMenuOpen] = useState(false);
  const [shouldClearInput, setShouldClearInput] = useState(false);

  // Handle clearing the input when removal is successful
  useEffect(() => {
    if (shouldClearInput && logoFileInputRef.current) {
      logoFileInputRef.current.value = "";
      setShouldClearInput(false);
    }
  }, [shouldClearInput, logoFileInputRef]);

  const handleLogoUpload = useCallback(
    async (files: FileList | null) => {
      const file = files?.[0];
      if (!file || !validateLogoFile(file)) {
        return;
      }

      // Create preview
      const objectUrl = URL.createObjectURL(file);
      setLogoPreviewUrl(objectUrl);

      // Upload immediately
      const formData = new FormData();
      formData.append("file", file);
      await updateTenantLogoMutation.mutateAsync({ body: formData });

      // Clean up preview after successful upload
      URL.revokeObjectURL(objectUrl);
      setLogoPreviewUrl(null);

      // Invalidate all queries to refresh UI
      await queryClient.invalidateQueries();
      refetchTenant();

      toast.success(t`Logo uploaded successfully`);
    },
    [updateTenantLogoMutation, refetchTenant, queryClient]
  );

  const handleLogoRemoval = useCallback(async () => {
    await removeTenantLogoMutation.mutateAsync({});

    // Invalidate all queries to refresh UI
    await queryClient.invalidateQueries();
    refetchTenant();

    // Trigger input clearing via state
    setShouldClearInput(true);

    toast.success(t`Logo removed successfully`);
  }, [removeTenantLogoMutation, refetchTenant, queryClient]);

  const cleanupLogoPreview = useCallback(() => {
    if (logoPreviewUrl) {
      URL.revokeObjectURL(logoPreviewUrl);
      setLogoPreviewUrl(null);
    }
  }, [logoPreviewUrl]);

  return {
    logoPreviewUrl,
    logoMenuOpen,
    setLogoMenuOpen,
    handleLogoUpload,
    handleLogoRemoval,
    cleanupLogoPreview
  };
}

// Logo management component
function LogoSection({
  tenant,
  logoPreviewUrl,
  logoMenuOpen,
  setLogoMenuOpen,
  handleLogoUpload,
  handleLogoRemoval,
  logoFileInputRef,
  isOwner
}: Readonly<{
  tenant: { logoUrl?: string | null; name?: string } | null | undefined;
  logoPreviewUrl: string | null;
  logoMenuOpen: boolean;
  setLogoMenuOpen: (open: boolean) => void;
  handleLogoUpload: (files: FileList | null) => void;
  handleLogoRemoval: () => void;
  logoFileInputRef: React.RefObject<HTMLInputElement | null>;
  isOwner: boolean;
}>) {
  return (
    <>
      <input
        type="file"
        ref={logoFileInputRef}
        onChange={(e) => {
          setLogoMenuOpen(false);
          handleLogoUpload(e.target.files);
        }}
        accept={ALLOWED_FILE_TYPES.join(",")}
        className="hidden"
      />

      <Field className="w-fit">
        <FieldLabel>
          <Trans>Logo</Trans>
        </FieldLabel>
        <DropdownMenu open={logoMenuOpen} onOpenChange={setLogoMenuOpen}>
          <DropdownMenuTrigger
            disabled={!isOwner}
            render={
              <Button
                variant="ghost"
                size="icon"
                className="size-16 rounded-md"
                aria-label={t`Change logo`}
                disabled={!isOwner}
              >
                <TenantLogo
                  key={logoPreviewUrl ?? tenant?.logoUrl ?? "no-logo"}
                  logoUrl={logoPreviewUrl ?? tenant?.logoUrl}
                  tenantName={tenant?.name ?? ""}
                  size="lg"
                />
              </Button>
            }
          />
          <DropdownMenuContent>
            <DropdownMenuItem
              onClick={() => {
                logoFileInputRef.current?.click();
              }}
            >
              <CameraIcon className="size-4" />
              <Trans>Upload logo</Trans>
            </DropdownMenuItem>
            {(tenant?.logoUrl || logoPreviewUrl) && (
              <>
                <DropdownMenuSeparator />
                <DropdownMenuItem
                  variant="destructive"
                  onClick={() => {
                    setLogoMenuOpen(false);
                    handleLogoRemoval();
                  }}
                >
                  <Trash2Icon className="size-4" />
                  <Trans>Remove logo</Trans>
                </DropdownMenuItem>
              </>
            )}
          </DropdownMenuContent>
        </DropdownMenu>
      </Field>
    </>
  );
}

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
  const logoFileInputRef = useRef<HTMLInputElement>(null);
  const queryClient = useQueryClient();

  const {
    data: tenant,
    isLoading: tenantLoading,
    refetch: refetchTenant
  } = api.useQuery("get", "/api/account-management/tenants/current");
  const { data: currentUser, isLoading: userLoading } = api.useQuery("get", "/api/account-management/users/me");
  const updateCurrentTenantMutation = api.useMutation("put", "/api/account-management/tenants/current", {
    onSuccess: () => {
      setIsFormDirty(false);
      toast.success(t`Account name updated successfully`);
      refetchTenant();
    }
  });
  const updateTenantLogoMutation = api.useMutation("post", "/api/account-management/tenants/current/update-logo");
  const removeTenantLogoMutation = api.useMutation("delete", "/api/account-management/tenants/current/remove-logo");

  const { logoPreviewUrl, logoMenuOpen, setLogoMenuOpen, handleLogoUpload, handleLogoRemoval } = useLogoManagement(
    updateTenantLogoMutation as unknown as FileUploadMutation,
    removeTenantLogoMutation,
    refetchTenant,
    queryClient,
    logoFileInputRef
  );

  const isOwner = currentUser?.role === UserRole.Owner;

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
      <FederatedSideMenu currentSystem="account-management" />
      <AppLayout
        variant="center"
        topMenu={
          <TopMenu>
            <BreadcrumbPage>
              <Trans>Account settings</Trans>
            </BreadcrumbPage>
          </TopMenu>
        }
        title={t`Account settings`}
        subtitle={t`Manage your account here.`}
      >
        <Form
          onSubmit={isOwner ? mutationSubmitter(updateCurrentTenantMutation) : undefined}
          validationErrors={isOwner ? updateCurrentTenantMutation.error?.errors : undefined}
          validationBehavior="aria"
          className="flex flex-col gap-4"
        >
          <h3>
            <Trans>Account information</Trans>
          </h3>
          <Separator />

          <LogoSection
            tenant={tenant}
            logoPreviewUrl={logoPreviewUrl}
            logoMenuOpen={logoMenuOpen}
            setLogoMenuOpen={setLogoMenuOpen}
            handleLogoUpload={handleLogoUpload}
            handleLogoRemoval={handleLogoRemoval}
            logoFileInputRef={logoFileInputRef}
            isOwner={isOwner}
          />

          <TextField
            isRequired={true}
            name="name"
            defaultValue={tenant?.name ?? ""}
            isDisabled={updateCurrentTenantMutation.isPending}
            isReadOnly={!isOwner}
            label={t`Account name`}
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
