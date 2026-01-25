import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { AuthenticationContext } from "@repo/infrastructure/auth/AuthenticationProvider";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { Button } from "@repo/ui/components/Button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger
} from "@repo/ui/components/DropdownMenu";
import { Form } from "@repo/ui/components/Form";
import { Label } from "@repo/ui/components/Label";
import { TextField } from "@repo/ui/components/TextField";
import { mutationSubmitter } from "@repo/ui/forms/mutationSubmitter";
import { useUnsavedChangesGuard } from "@repo/ui/hooks/useUnsavedChangesGuard";
import type { FileUploadMutation } from "@repo/ui/types/FileUpload";
import { useMutation } from "@tanstack/react-query";
import { createFileRoute } from "@tanstack/react-router";
import { CameraIcon, MailIcon, Trash2Icon } from "lucide-react";
import { useContext, useRef, useState } from "react";
import { toast } from "sonner";
import FederatedSideMenu from "@/federated-modules/sideMenu/FederatedSideMenu";
import { UnsavedChangesDialog } from "@/shared/components/UnsavedChangesDialog";
import { api, type Schemas } from "@/shared/lib/api/client";

export const Route = createFileRoute("/account/profile/")({
  component: ProfilePage
});

const MAX_FILE_SIZE = 1024 * 1024; // 1MB in bytes
const ALLOWED_FILE_TYPES = ["image/jpeg", "image/png", "image/gif", "image/webp"];

function ProfilePage() {
  const [selectedAvatarFile, setSelectedAvatarFile] = useState<File | null>(null);
  const [avatarPreviewUrl, setAvatarPreviewUrl] = useState<string | null>(null);
  const [avatarMenuOpen, setAvatarMenuOpen] = useState(false);
  const [removeAvatarFlag, setRemoveAvatarFlag] = useState(false);
  const [isFormDirty, setIsFormDirty] = useState(false);

  const avatarFileInputRef = useRef<HTMLInputElement>(null);

  const { updateUserInfo } = useContext(AuthenticationContext);

  const { data: user, isLoading: isLoadingUser, refetch } = api.useQuery("get", "/api/account/users/me");

  const updateAvatarMutation = api.useMutation("post", "/api/account/users/me/update-avatar");
  const removeAvatarMutation = api.useMutation("delete", "/api/account/users/me/remove-avatar");
  const updateCurrentUserMutation = api.useMutation("put", "/api/account/users/me");

  const saveMutation = useMutation<
    void,
    Schemas["HttpValidationProblemDetails"],
    { body: Schemas["UpdateCurrentUserCommand"] }
  >({
    mutationFn: async (data) => {
      if (selectedAvatarFile) {
        const formData = new FormData();
        formData.append("file", selectedAvatarFile);
        await (updateAvatarMutation as unknown as FileUploadMutation).mutateAsync({ body: formData });
      } else if (removeAvatarFlag) {
        await removeAvatarMutation.mutateAsync({});
        setRemoveAvatarFlag(false);
      }

      await updateCurrentUserMutation.mutateAsync(data);

      const { data: updatedUser } = await refetch();
      if (updatedUser) {
        updateUserInfo(updatedUser);
      }
    },
    onSuccess: () => {
      setSelectedAvatarFile(null);
      setAvatarPreviewUrl(null);
      setRemoveAvatarFlag(false);
      setIsFormDirty(false);
      toast.success(t`Profile updated successfully`);
    }
  });

  const { isConfirmDialogOpen, confirmLeave, cancelLeave } = useUnsavedChangesGuard({
    hasUnsavedChanges: isFormDirty
  });

  const onFileSelect = (files: FileList | null) => {
    if (files?.[0]) {
      const file = files[0];

      if (!ALLOWED_FILE_TYPES.includes(file.type)) {
        alert(t`Please select a JPEG, PNG, GIF, or WebP image.`);
        return;
      }

      if (file.size > MAX_FILE_SIZE) {
        alert(t`Image must be smaller than 1 MB.`);
        return;
      }

      setSelectedAvatarFile(file);
      const objectUrl = URL.createObjectURL(file);
      setAvatarPreviewUrl(objectUrl);
      setRemoveAvatarFlag(false);
      setIsFormDirty(true);
    }
  };

  if (isLoadingUser) {
    return (
      <>
        <FederatedSideMenu currentSystem="account" />
        <AppLayout variant="center">
          <div className="flex flex-1 items-center justify-center">
            <Trans>Loading...</Trans>
          </div>
        </AppLayout>
      </>
    );
  }

  if (!user) {
    return (
      <>
        <FederatedSideMenu currentSystem="account" />
        <AppLayout variant="center">
          <div className="flex flex-1 items-center justify-center">
            <Trans>Unable to load profile</Trans>
          </div>
        </AppLayout>
      </>
    );
  }

  return (
    <>
      <FederatedSideMenu currentSystem="account" />
      <AppLayout
        variant="center"
        title={t`Profile`}
        subtitle={t`Update your profile picture and personal details here.`}
      >
        <Form
          onSubmit={mutationSubmitter(saveMutation)}
          validationBehavior="aria"
          validationErrors={saveMutation.error?.errors}
          className="flex flex-col gap-4"
          onChange={() => setIsFormDirty(true)}
        >
          <input
            type="file"
            ref={avatarFileInputRef}
            onChange={(e) => {
              setAvatarMenuOpen(false);
              onFileSelect(e.target.files);
            }}
            accept={ALLOWED_FILE_TYPES.join(",")}
            className="hidden"
          />

          <div className="flex flex-col gap-2">
            <Label>
              <Trans>Profile picture</Trans>
            </Label>

            <DropdownMenu open={avatarMenuOpen} onOpenChange={setAvatarMenuOpen}>
              <DropdownMenuTrigger
                render={
                  <Button
                    variant="ghost"
                    size="icon"
                    className="size-20 rounded-full bg-secondary hover:bg-secondary/80"
                    aria-label={t`Change profile picture`}
                  >
                    {user.avatarUrl || avatarPreviewUrl ? (
                      <img
                        src={avatarPreviewUrl ?? user.avatarUrl ?? ""}
                        className="size-full rounded-full object-cover"
                        alt={t`Profile avatar`}
                      />
                    ) : (
                      <CameraIcon className="size-10 text-secondary-foreground" aria-label={t`Add profile picture`} />
                    )}
                  </Button>
                }
              />
              <DropdownMenuContent>
                <DropdownMenuItem
                  onClick={() => {
                    avatarFileInputRef.current?.click();
                  }}
                >
                  <CameraIcon className="size-4" />
                  <Trans>Upload profile picture</Trans>
                </DropdownMenuItem>
                {(user.avatarUrl || avatarPreviewUrl) && (
                  <>
                    <DropdownMenuSeparator />
                    <DropdownMenuItem
                      variant="destructive"
                      onClick={() => {
                        setAvatarMenuOpen(false);
                        setRemoveAvatarFlag(true);
                        setSelectedAvatarFile(null);
                        setAvatarPreviewUrl(null);
                        setIsFormDirty(true);
                        user.avatarUrl = null;
                      }}
                    >
                      <Trash2Icon className="size-4" />
                      <Trans>Remove profile picture</Trans>
                    </DropdownMenuItem>
                  </>
                )}
              </DropdownMenuContent>
            </DropdownMenu>
          </div>

          <div className="flex flex-col gap-4 sm:flex-row">
            <TextField
              isRequired={true}
              name="firstName"
              label={t`First name`}
              defaultValue={user?.firstName}
              placeholder={t`E.g. Alex`}
              className="sm:flex-1"
            />
            <TextField
              isRequired={true}
              name="lastName"
              label={t`Last name`}
              defaultValue={user?.lastName}
              placeholder={t`E.g. Taylor`}
              className="sm:flex-1"
            />
          </div>

          <TextField
            name="email"
            label={t`Email`}
            value={user?.email}
            isDisabled={true}
            startIcon={<MailIcon className="size-4" />}
          />

          <TextField
            name="title"
            label={t`Title`}
            tooltip={t`Your professional title or role`}
            defaultValue={user?.title}
            placeholder={t`E.g. Software engineer`}
          />

          <div className="mt-4 flex">
            <Button type="submit" disabled={saveMutation.isPending}>
              {saveMutation.isPending ? <Trans>Saving...</Trans> : <Trans>Save changes</Trans>}
            </Button>
          </div>
        </Form>
      </AppLayout>

      <UnsavedChangesDialog isOpen={isConfirmDialogOpen} onConfirmLeave={confirmLeave} onCancel={cancelLeave} />
    </>
  );
}
