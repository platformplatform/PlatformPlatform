import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { AuthenticationContext } from "@repo/infrastructure/auth/AuthenticationProvider";
import { Button } from "@repo/ui/components/Button";
import {
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle
} from "@repo/ui/components/Dialog";
import { DirtyDialog } from "@repo/ui/components/DirtyDialog";
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
import { toastQueue } from "@repo/ui/components/Toast";
import { mutationSubmitter } from "@repo/ui/forms/mutationSubmitter";
import type { FileUploadMutation } from "@repo/ui/types/FileUpload";
import { useMutation } from "@tanstack/react-query";
import { CameraIcon, MailIcon, Trash2Icon } from "lucide-react";
import { useCallback, useContext, useRef, useState } from "react";
import { FileTrigger } from "react-aria-components";
import { api, type Schemas } from "@/shared/lib/api/client";

const MAX_FILE_SIZE = 1024 * 1024; // 1MB in bytes
const ALLOWED_FILE_TYPES = ["image/jpeg", "image/png", "image/gif", "image/webp"]; // Align with backend

type ProfileModalProps = {
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
};

export default function UserProfileModal({ isOpen, onOpenChange }: Readonly<ProfileModalProps>) {
  const [selectedAvatarFile, setSelectedAvatarFile] = useState<File | null>(null);
  const [avatarPreviewUrl, setAvatarPreviewUrl] = useState<string | null>(null);
  const [avatarMenuOpen, setAvatarMenuOpen] = useState(false);
  const [removeAvatarFlag, setRemoveAvatarFlag] = useState(false);
  const [isFormDirty, setIsFormDirty] = useState(false);

  const avatarFileInputRef = useRef<HTMLInputElement>(null);

  const { updateUserInfo } = useContext(AuthenticationContext);

  const handleCloseComplete = useCallback(() => {
    setSelectedAvatarFile(null);
    setAvatarPreviewUrl(null);
    setRemoveAvatarFlag(false);
    setIsFormDirty(false);
  }, []);

  const {
    data: user,
    isLoading: isLoadingUser,
    error,
    refetch
  } = api.useQuery("get", "/api/account-management/users/me");

  const updateAvatarMutation = api.useMutation("post", "/api/account-management/users/me/update-avatar");
  const removeAvatarMutation = api.useMutation("delete", "/api/account-management/users/me/remove-avatar");
  const updateCurrentUserMutation = api.useMutation("put", "/api/account-management/users/me");

  const saveMutation = useMutation<
    void,
    Schemas["HttpValidationProblemDetails"],
    { body: Schemas["UpdateCurrentUserCommand"] }
  >({
    mutationFn: async (data) => {
      // Handle avatar changes first
      if (selectedAvatarFile) {
        const formData = new FormData();
        formData.append("file", selectedAvatarFile);
        await (updateAvatarMutation as unknown as FileUploadMutation).mutateAsync({ body: formData });
      } else if (removeAvatarFlag) {
        await removeAvatarMutation.mutateAsync({});
        setRemoveAvatarFlag(false);
      }

      // Update user profile data
      await updateCurrentUserMutation.mutateAsync(data);

      // Refetch to get the updated user data including new avatar URL
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
      toastQueue.add({
        title: t`Success`,
        description: t`Profile updated successfully`,
        variant: "success"
      });
      onOpenChange(false);
    }
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
    }
  };

  if (!isOpen) {
    return null;
  }

  const hasUnsavedChanges = isFormDirty || selectedAvatarFile !== null || removeAvatarFlag;

  return (
    <DirtyDialog
      open={isOpen}
      onOpenChange={onOpenChange}
      hasUnsavedChanges={hasUnsavedChanges}
      unsavedChangesTitle={t`Unsaved changes`}
      unsavedChangesMessage={<Trans>You have unsaved changes. If you leave now, your changes will be lost.</Trans>}
      leaveLabel={t`Leave`}
      stayLabel={t`Stay`}
      onCloseComplete={handleCloseComplete}
    >
      {!user ? (
        <DialogContent className="sm:w-dialog-md">
          <DialogHeader>
            <DialogTitle>
              {isLoadingUser && <Trans>Fetching data...</Trans>}
              {error && JSON.stringify(error)}
            </DialogTitle>
          </DialogHeader>
        </DialogContent>
      ) : (
        <DialogContent className="sm:w-dialog-lg sm:max-w-none">
          <DialogHeader>
            <DialogTitle>
              <Trans>User profile</Trans>
            </DialogTitle>
            <DialogDescription>
              <Trans>Update your profile picture and personal details here.</Trans>
            </DialogDescription>
          </DialogHeader>

          <Form
            onSubmit={mutationSubmitter(saveMutation)}
            validationBehavior="aria"
            validationErrors={saveMutation.error?.errors}
            className="flex min-h-0 flex-1 flex-col"
          >
            <div className="flex flex-col gap-4">
              <FileTrigger
                ref={avatarFileInputRef}
                onSelect={(files) => {
                  setAvatarMenuOpen(false);
                  onFileSelect(files);
                }}
                acceptedFileTypes={ALLOWED_FILE_TYPES}
              />

              <Label>
                <Trans>Profile picture</Trans>
              </Label>

              <DropdownMenu open={avatarMenuOpen} onOpenChange={setAvatarMenuOpen}>
                <DropdownMenuTrigger
                  render={
                    <Button
                      variant="ghost"
                      size="icon"
                      className="mb-3 h-16 w-16 rounded-full bg-secondary hover:bg-secondary/80"
                      aria-label={t`Change profile picture`}
                    >
                      {user.avatarUrl || avatarPreviewUrl ? (
                        <img
                          src={avatarPreviewUrl ?? user.avatarUrl ?? ""}
                          className="h-full w-full rounded-full object-cover"
                          alt={t`Preview avatar`}
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
                    <CameraIcon className="h-4 w-4" />
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
                          user.avatarUrl = null;
                        }}
                      >
                        <Trash2Icon className="h-4 w-4" />
                        <Trans>Remove profile picture</Trans>
                      </DropdownMenuItem>
                    </>
                  )}
                </DropdownMenuContent>
              </DropdownMenu>

              <div className="flex flex-col gap-4 sm:flex-row">
                <TextField
                  isRequired={true}
                  name="firstName"
                  label={t`First name`}
                  defaultValue={user?.firstName}
                  placeholder={t`E.g. Alex`}
                  className="sm:flex-1"
                  onChange={() => setIsFormDirty(true)}
                />
                <TextField
                  isRequired={true}
                  name="lastName"
                  label={t`Last name`}
                  defaultValue={user?.lastName}
                  placeholder={t`E.g. Taylor`}
                  className="sm:flex-1"
                  onChange={() => setIsFormDirty(true)}
                />
              </div>
              <TextField
                name="email"
                label={t`Email`}
                value={user?.email}
                isDisabled={true}
                startIcon={<MailIcon className="h-4 w-4" />}
              />
              <TextField
                name="title"
                label={t`Title`}
                tooltip={t`Your professional title or role`}
                defaultValue={user?.title}
                placeholder={t`E.g. Software engineer`}
                onChange={() => setIsFormDirty(true)}
              />
            </div>

            <DialogFooter className="pt-6">
              <DialogClose
                render={<Button type="reset" variant="secondary" disabled={isLoadingUser || saveMutation.isPending} />}
              >
                <Trans>Cancel</Trans>
              </DialogClose>
              <Button type="submit" disabled={isLoadingUser || saveMutation.isPending}>
                {saveMutation.isPending ? <Trans>Saving...</Trans> : <Trans>Save changes</Trans>}
              </Button>
            </DialogFooter>
          </Form>
        </DialogContent>
      )}
    </DirtyDialog>
  );
}
