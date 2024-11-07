import { useCallback, useEffect, useRef, useState } from "react";
import { useFormState } from "react-dom";
import { FileTrigger, Form, Heading, Label } from "react-aria-components";
import { Menu, MenuItem, MenuSeparator, MenuTrigger } from "@repo/ui/components/Menu";
import { CameraIcon, Trash2Icon, XIcon } from "lucide-react";
import { Button } from "@repo/ui/components/Button";
import { Dialog } from "@repo/ui/components/Dialog";
import { FormErrorMessage } from "@repo/ui/components/FormErrorMessage";
import { Modal } from "@repo/ui/components/Modal";
import { TextField } from "@repo/ui/components/TextField";
import type { Schemas } from "@/shared/lib/api/client";
import { api } from "@/shared/lib/api/client";
import { t, Trans } from "@lingui/macro";

const MAX_FILE_SIZE = 1024 * 1024; // 1MB in bytes
const ALLOWED_FILE_TYPES = ["image/jpeg", "image/png", "image/gif", "image/webp"]; // Align with backend

type ProfileModalProps = {
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
  userId: string;
};

export default function UserProfileModal({ isOpen, onOpenChange, userId }: Readonly<ProfileModalProps>) {
  const [data, setData] = useState<Schemas["UserResponse"] | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [selectedAvatarFile, setSelectedAvatarFile] = useState<File | null>(null);
  const [avatarPreviewUrl, setAvatarPreviewUrl] = useState<string | null>(null);
  const [avatarMenuOpen, setAvatarMenuOpen] = useState(false);
  const [removeAvatarFlag, setRemoveAvatarFlag] = useState(false);

  const avatarFileInputRef = useRef<HTMLInputElement>(null);

  // Fetch user data when modal opens
  useEffect(() => {
    if (isOpen) {
      setLoading(true);
      setData(null);
      setError(null);
      setSelectedAvatarFile(null);
      setAvatarPreviewUrl(null);
      setAvatarMenuOpen(false);
      setRemoveAvatarFlag(false);

      api
        .get("/api/account-management/users/{id}", { params: { path: { id: userId } } })
        .then((response) => setData(response))
        .catch((error) => setError(error))
        .finally(() => setLoading(false));
    }
  }, [isOpen, userId]);

  // Close dialog and cleanup
  const closeDialog = useCallback(() => {
    onOpenChange(false);
    setSelectedAvatarFile(null);
    if (avatarPreviewUrl) {
      URL.revokeObjectURL(avatarPreviewUrl);
      setAvatarPreviewUrl(null);
    }
  }, [onOpenChange, avatarPreviewUrl]);

  // Handle form submission
  let [{ success, errors, title, message }, action, isPending] = useFormState(
    api.actionPut("/api/account-management/users/{id}"),
    { success: null }
  );

  const handleFormSubmit = async (formData: FormData) => {
    if (selectedAvatarFile) {
      await api.uploadFile("/api/account-management/users/update-avatar", selectedAvatarFile);
    } else if (removeAvatarFlag) {
      await api.delete("/api/account-management/users/remove-avatar");
      setRemoveAvatarFlag(false);
    }
    action(formData);
  };

  useEffect(() => {
    if (isPending) {
      success = undefined;
    }

    if (success) {
      closeDialog();
      api
        .post("/api/account-management/authentication/refresh-authentication-tokens")
        .then(() => window.location.reload());
    }
  }, [success, isPending, closeDialog]);

  // Handle file selection
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

  return (
    <Modal isOpen={isOpen} onOpenChange={onOpenChange} isDismissable={!loading}>
      <Dialog>
        {!data && (
          <Heading slot="title">
            {loading && <Trans>Fetching data...</Trans>}
            {error && JSON.stringify(error)}
          </Heading>
        )}

        {data && (
          <>
            <XIcon onClick={closeDialog} className="h-10 w-10 absolute top-2 right-2 p-2 hover:bg-muted" />
            <Heading slot="title" className="text-2xl">
              <Trans>User profile</Trans>
            </Heading>
            <p className="text-muted-foreground text-sm">
              <Trans>Update your photo and personal details here.</Trans>
            </p>

            <Form
              action={handleFormSubmit}
              validationErrors={errors}
              validationBehavior="aria"
              className="flex flex-col gap-4 mt-4"
            >
              <input type="hidden" name="id" value={userId} />
              <FileTrigger
                ref={avatarFileInputRef}
                onSelect={(files) => {
                  setAvatarMenuOpen(false);
                  onFileSelect(files);
                }}
                acceptedFileTypes={ALLOWED_FILE_TYPES}
              />

              <Label>
                <Trans>Photo</Trans>
              </Label>

              <MenuTrigger isOpen={avatarMenuOpen} onOpenChange={setAvatarMenuOpen}>
                <Button
                  variant="icon"
                  className="rounded-full w-16 h-16 mb-3 bg-secondary hover:bg-secondary/80"
                  aria-label={t`Change avatar options`}
                >
                  {data.avatarUrl || avatarPreviewUrl ? (
                    <img
                      src={avatarPreviewUrl ?? data.avatarUrl ?? ""}
                      className="rounded-full h-full w-full object-cover"
                      alt={t`Preview avatar`}
                    />
                  ) : (
                    <CameraIcon className="size-10 text-secondary-foreground" aria-label={t`Add avatar`} />
                  )}
                </Button>
                <Menu>
                  <MenuItem
                    onAction={() => {
                      avatarFileInputRef.current?.click();
                    }}
                  >
                    <CameraIcon className="w-4 h-4" />
                    <Trans>Upload photo</Trans>
                  </MenuItem>
                  {(data.avatarUrl || avatarPreviewUrl) && (
                    <>
                      <MenuSeparator />
                      <MenuItem
                        onAction={() => {
                          setAvatarMenuOpen(false);
                          setRemoveAvatarFlag(true);
                          setSelectedAvatarFile(null);
                          setAvatarPreviewUrl(null);
                          data.avatarUrl = null;
                        }}
                      >
                        <Trash2Icon className="w-4 h-4 text-destructive" />
                        <span className="text-destructive">
                          <Trans>Remove photo</Trans>
                        </span>
                      </MenuItem>
                    </>
                  )}
                </Menu>
              </MenuTrigger>

              <div className="flex flex-col sm:flex-row gap-4">
                <TextField
                  autoFocus
                  isRequired
                  name="firstName"
                  label={t`First name`}
                  defaultValue={data.firstName}
                  placeholder={t`E.g., Olivia`}
                  className="sm:w-64"
                />
                <TextField
                  isRequired
                  name="lastName"
                  label={t`Last name`}
                  defaultValue={data.lastName}
                  placeholder={t`E.g., Rhye`}
                  className="sm:w-64"
                />
              </div>
              <TextField name="email" label={t`Email`} value={data?.email} />
              <TextField
                name="title"
                label={t`Title`}
                defaultValue={data?.title}
                placeholder={t`E.g., Marketing Manager`}
              />

              <FormErrorMessage title={title} message={message} />

              <div className="flex justify-end gap-4 mt-6">
                <Button type="reset" onPress={closeDialog} variant="secondary">
                  <Trans>Cancel</Trans>
                </Button>
                <Button type="submit" isDisabled={isPending}>
                  <Trans>Save changes</Trans>
                </Button>
              </div>
            </Form>
          </>
        )}
      </Dialog>
    </Modal>
  );
}
