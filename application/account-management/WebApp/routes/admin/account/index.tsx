import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { Breadcrumb } from "@repo/ui/components/Breadcrumbs";
import { Button } from "@repo/ui/components/Button";
import { Form } from "@repo/ui/components/Form";
import { Menu, MenuItem, MenuSeparator, MenuTrigger } from "@repo/ui/components/Menu";
import { TenantLogo } from "@repo/ui/components/TenantLogo";
import { TextField } from "@repo/ui/components/TextField";
import { toastQueue } from "@repo/ui/components/Toast";
import { mutationSubmitter } from "@repo/ui/forms/mutationSubmitter";
import type { FileUploadMutation } from "@repo/ui/types/FileUpload";
import { useQueryClient } from "@tanstack/react-query";
import { createFileRoute } from "@tanstack/react-router";
import { CameraIcon, Trash2, Trash2Icon } from "lucide-react";
import type React from "react";
import { useCallback, useEffect, useRef, useState } from "react";
import { FileTrigger, Label, Separator } from "react-aria-components";
import FederatedSideMenu from "@/federated-modules/sideMenu/FederatedSideMenu";
import { TopMenu } from "@/shared/components/topMenu";
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

      toastQueue.add({
        title: t`Success`,
        description: t`Logo uploaded successfully`,
        variant: "success"
      });
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

    toastQueue.add({
      title: t`Success`,
      description: t`Logo removed successfully`,
      variant: "success"
    });
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
      <FileTrigger
        ref={logoFileInputRef}
        onSelect={(files) => {
          setLogoMenuOpen(false);
          handleLogoUpload(files);
        }}
        acceptedFileTypes={ALLOWED_FILE_TYPES}
      />

      <Label>
        <Trans>Logo</Trans>
      </Label>

      <MenuTrigger isOpen={logoMenuOpen} onOpenChange={setLogoMenuOpen}>
        <Button variant="icon" className="h-16 w-16 rounded-md" aria-label={t`Change logo`} isDisabled={!isOwner}>
          {tenant?.logoUrl || logoPreviewUrl ? (
            <img
              src={logoPreviewUrl ?? tenant?.logoUrl ?? ""}
              className="h-full w-full rounded-md object-contain"
              alt={t`Logo`}
            />
          ) : (
            <TenantLogo
              key={tenant?.logoUrl || "no-logo"}
              logoUrl={null}
              tenantName={tenant?.name ?? ""}
              size="lg"
              isRound={false}
              className="h-full w-full"
            />
          )}
        </Button>
        <Menu>
          <MenuItem
            onAction={() => {
              logoFileInputRef.current?.click();
            }}
          >
            <CameraIcon className="h-4 w-4" />
            <Trans>Upload logo</Trans>
          </MenuItem>
          {(tenant?.logoUrl || logoPreviewUrl) && (
            <>
              <MenuSeparator />
              <MenuItem
                onAction={() => {
                  setLogoMenuOpen(false);
                  handleLogoRemoval();
                }}
              >
                <Trash2Icon className="h-4 w-4 text-destructive" />
                <span className="text-destructive">
                  <Trans>Remove logo</Trans>
                </span>
              </MenuItem>
            </>
          )}
        </Menu>
      </MenuTrigger>
    </>
  );
}

// Danger zone component
function DangerZone({ setIsDeleteModalOpen }: { setIsDeleteModalOpen: (open: boolean) => void }) {
  return (
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
  );
}

export function AccountSettings() {
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const logoFileInputRef = useRef<HTMLInputElement>(null);
  const queryClient = useQueryClient();

  const {
    data: tenant,
    isLoading: tenantLoading,
    refetch: refetchTenant
  } = api.useQuery("get", "/api/account-management/tenants/current");
  const { data: currentUser, isLoading: userLoading } = api.useQuery("get", "/api/account-management/users/me");
  const updateCurrentTenantMutation = api.useMutation("put", "/api/account-management/tenants/current");
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

  useEffect(() => {
    if (updateCurrentTenantMutation.isSuccess) {
      toastQueue.add({
        title: t`Success`,
        description: t`Account name updated successfully`,
        variant: "success"
      });
      refetchTenant();
    }
  }, [updateCurrentTenantMutation.isSuccess, refetchTenant]);

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
            <Breadcrumb href="/admin/account">
              <Trans>Account</Trans>
            </Breadcrumb>
            <Breadcrumb>
              <Trans>Settings</Trans>
            </Breadcrumb>
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
          <h2>
            <Trans>Account information</Trans>
          </h2>
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
            description={!isOwner ? t`Only account owners can modify the account name` : undefined}
            validationBehavior="aria"
          />
          {isOwner && (
            <Button type="submit" className="mt-4" isDisabled={updateCurrentTenantMutation.isPending}>
              {updateCurrentTenantMutation.isPending ? <Trans>Saving...</Trans> : <Trans>Save changes</Trans>}
            </Button>
          )}
        </Form>

        {isOwner && <DangerZone setIsDeleteModalOpen={setIsDeleteModalOpen} />}
      </AppLayout>

      <DeleteAccountConfirmation isOpen={isDeleteModalOpen} onOpenChange={setIsDeleteModalOpen} />
    </>
  );
}
