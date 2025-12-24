import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { AuthenticationContext } from "@repo/infrastructure/auth/AuthenticationProvider";
import { Button } from "@repo/ui/components/Button";
import { Dialog } from "@repo/ui/components/Dialog";
import { DialogContent, DialogFooter, DialogHeader } from "@repo/ui/components/DialogFooter";
import { Heading } from "@repo/ui/components/Heading";
import { Menu, MenuItem, MenuSeparator, MenuTrigger } from "@repo/ui/components/Menu";
import { MenuButton } from "@repo/ui/components/MenuButton";
import { TextField } from "@repo/ui/components/TextField";
import { toastQueue } from "@repo/ui/components/Toast";
import { mutationSubmitter } from "@repo/ui/forms/mutationSubmitter";
import type { FileUploadMutation } from "@repo/ui/types/FileUpload";
import { useMutation } from "@tanstack/react-query";
import { CameraIcon, MailIcon, Trash2Icon, XIcon } from "lucide-react";
import { useContext, useEffect, useRef, useState } from "react";
import { FileTrigger, Form, Label } from "react-aria-components";
import { DirtyModal } from "@/shared/components/DirtyModal";
import { api, type Schemas } from "@/shared/lib/api/client";

const MAX_FILE_SIZE = 1024 * 1024; // 1MB in bytes
const ALLOWED_FILE_TYPES = ["image/jpeg", "image/png", "image/gif", "image/webp"]; // Align with backend

type ProfileModalProps = {
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
};

export default function UserProfileModal({ isOpen, onOpenChange }: Readonly<ProfileModalProps>) {
  const [isLoading, setIsLoading] = useState(false);
  const [selectedAvatarFile, setSelectedAvatarFile] = useState<File | null>(null);
  const [avatarPreviewUrl, setAvatarPreviewUrl] = useState<string | null>(null);
  const [avatarMenuOpen, setAvatarMenuOpen] = useState(false);
  const [removeAvatarFlag, setRemoveAvatarFlag] = useState(false);
  const [isFormDirty, setIsFormDirty] = useState(false);

  const avatarFileInputRef = useRef<HTMLInputElement>(null);

  const { updateUserInfo } = useContext(AuthenticationContext);

  const {
    data: user,
    isLoading: isLoadingUser,
    error,
    refetch
  } = api.useQuery("get", "/api/account-management/users/me");

  const hasUnsavedChanges = isFormDirty || selectedAvatarFile !== null || removeAvatarFlag;

  useEffect(() => {
    setIsLoading(isLoadingUser);
  }, [isLoadingUser]);

  const handleCloseComplete = () => {
    setSelectedAvatarFile(null);
    setRemoveAvatarFlag(false);
    setIsFormDirty(false);
    if (avatarPreviewUrl) {
      URL.revokeObjectURL(avatarPreviewUrl);
      setAvatarPreviewUrl(null);
    }
  };

  const handleCancel = () => {
    handleCloseComplete();
    onOpenChange(false);
  };

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

  return (
    <DirtyModal
      isOpen={isOpen}
      onOpenChange={onOpenChange}
      hasUnsavedChanges={hasUnsavedChanges}
      isDismissable={!isLoading && !saveMutation.isPending}
      onCloseComplete={handleCloseComplete}
      zIndex="high"
    >
      {!user ? (
        <Dialog aria-label={t`User profile`}>
          <Heading slot="title">
            {isLoadingUser && <Trans>Fetching data...</Trans>}
            {error && JSON.stringify(error)}
          </Heading>
        </Dialog>
      ) : (
        <Dialog
          aria-label={t`User profile`}
          className="max-sm:flex max-sm:flex-col max-sm:overflow-hidden sm:w-dialog-lg"
        >
          {({ close }) => (
            <>
              <XIcon onClick={close} className="absolute top-2 right-2 h-10 w-10 cursor-pointer p-2 hover:bg-muted" />
              <DialogHeader description={<Trans>Update your profile picture and personal details here.</Trans>}>
                <Heading slot="title" className="text-2xl">
                  <Trans>User profile</Trans>
                </Heading>
              </DialogHeader>

              <Form
                onSubmit={mutationSubmitter(saveMutation)}
                validationBehavior="aria"
                validationErrors={saveMutation.error?.errors}
                className="flex min-h-0 flex-1 flex-col"
              >
                <DialogContent className="flex flex-col gap-4">
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

                  <MenuTrigger isOpen={avatarMenuOpen} onOpenChange={setAvatarMenuOpen}>
                    <MenuButton
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
                    </MenuButton>
                    <Menu>
                      <MenuItem
                        onAction={() => {
                          avatarFileInputRef.current?.click();
                        }}
                      >
                        <CameraIcon className="h-4 w-4" />
                        <Trans>Upload profile picture</Trans>
                      </MenuItem>
                      {(user.avatarUrl || avatarPreviewUrl) && (
                        <>
                          <MenuSeparator />
                          <MenuItem
                            onAction={() => {
                              setAvatarMenuOpen(false);
                              setRemoveAvatarFlag(true);
                              setSelectedAvatarFile(null);
                              setAvatarPreviewUrl(null);
                              user.avatarUrl = null;
                            }}
                          >
                            <Trash2Icon className="h-4 w-4 text-destructive" />
                            <span className="text-destructive">
                              <Trans>Remove profile picture</Trans>
                            </span>
                          </MenuItem>
                        </>
                      )}
                    </Menu>
                  </MenuTrigger>

                  <div className="flex flex-col gap-4 sm:flex-row">
                    <TextField
                      isRequired={true}
                      name="firstName"
                      label={t`First name`}
                      defaultValue={user?.firstName}
                      placeholder={t`E.g. Alex`}
                      className="sm:w-64"
                      onChange={() => setIsFormDirty(true)}
                    />
                    <TextField
                      isRequired={true}
                      name="lastName"
                      label={t`Last name`}
                      defaultValue={user?.lastName}
                      placeholder={t`E.g. Taylor`}
                      className="sm:w-64"
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
                </DialogContent>

                <DialogFooter>
                  <Button
                    type="reset"
                    onClick={handleCancel}
                    variant="secondary"
                    disabled={isLoading || saveMutation.isPending}
                  >
                    <Trans>Cancel</Trans>
                  </Button>
                  <Button type="submit" disabled={isLoading || saveMutation.isPending}>
                    {saveMutation.isPending ? <Trans>Saving...</Trans> : <Trans>Save changes</Trans>}
                  </Button>
                </DialogFooter>
              </Form>
            </>
          )}
        </Dialog>
      )}
    </DirtyModal>
  );
}
