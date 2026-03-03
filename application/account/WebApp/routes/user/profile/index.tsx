import type { FileUploadMutation } from "@repo/ui/types/FileUpload";

import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { AuthenticationContext } from "@repo/infrastructure/auth/AuthenticationProvider";
import { useUser } from "@repo/infrastructure/sync/hooks";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { Button } from "@repo/ui/components/Button";
import { Form } from "@repo/ui/components/Form";
import { mutationSubmitter } from "@repo/ui/forms/mutationSubmitter";
import { useUnsavedChangesGuard } from "@repo/ui/hooks/useUnsavedChangesGuard";
import { useMutation } from "@tanstack/react-query";
import { createFileRoute } from "@tanstack/react-router";
import { useContext, useEffect, useState } from "react";
import { toast } from "sonner";

import { UnsavedChangesDialog } from "@/shared/components/UnsavedChangesDialog";
import { UserProfileFields } from "@/shared/components/UserProfileFields";
import { api, type Schemas } from "@/shared/lib/api/client";

export const Route = createFileRoute("/user/profile/")({
  staticData: { trackingTitle: "User profile" },
  component: ProfilePage
});

function ProfilePage() {
  const [selectedAvatarFile, setSelectedAvatarFile] = useState<File | null>(null);
  const [removeAvatarFlag, setRemoveAvatarFlag] = useState(false);
  const [isFormDirty, setIsFormDirty] = useState(false);
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [title, setTitle] = useState("");

  const { updateUserInfo } = useContext(AuthenticationContext);

  const { id: userId } = import.meta.user_info_env;
  const { data: user } = useUser(userId ?? "");

  useEffect(() => {
    if (!isFormDirty) {
      if (user?.firstName !== undefined) {
        setFirstName(user.firstName ?? "");
      }
      if (user?.lastName !== undefined) {
        setLastName(user.lastName ?? "");
      }
      if (user?.title !== undefined) {
        setTitle(user.title ?? "");
      }
    }
  }, [user?.firstName, user?.lastName, user?.title, isFormDirty]);

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
      }

      await updateCurrentUserMutation.mutateAsync(data);
    },
    onSuccess: () => {
      setSelectedAvatarFile(null);
      setRemoveAvatarFlag(false);
      setIsFormDirty(false);
      updateUserInfo({
        firstName: firstName || undefined,
        lastName: lastName || undefined,
        title: title || undefined
      });
      toast.success(t`Profile updated successfully`);
    }
  });

  const { isConfirmDialogOpen, confirmLeave, cancelLeave } = useUnsavedChangesGuard({
    hasUnsavedChanges: isFormDirty
  });

  const handleAvatarFileSelect = (file: File | null) => {
    setSelectedAvatarFile(file);
    setRemoveAvatarFlag(false);
    setIsFormDirty(true);
  };

  const handleAvatarRemove = () => {
    setRemoveAvatarFlag(true);
    setIsFormDirty(true);
  };

  return (
    <>
      <AppLayout
        variant="center"
        maxWidth="64rem"
        balanceWidth="16rem"
        title={t`User profile`}
        subtitle={t`Update your profile picture and personal details here.`}
      >
        <Form
          onSubmit={mutationSubmitter(saveMutation)}
          validationBehavior="aria"
          validationErrors={saveMutation.error?.errors}
          className="flex flex-col gap-4"
          onChange={() => setIsFormDirty(true)}
        >
          <UserProfileFields
            layout="horizontal"
            user={user}
            isPending={saveMutation.isPending}
            onAvatarFileSelect={handleAvatarFileSelect}
            onAvatarRemove={handleAvatarRemove}
            firstNameValue={firstName}
            lastNameValue={lastName}
            titleValue={title}
            onFirstNameChange={setFirstName}
            onLastNameChange={setLastName}
            onTitleChange={setTitle}
            onChange={() => setIsFormDirty(true)}
          />

          <div className="mt-4 md:grid md:grid-cols-[8.5rem_1fr] md:gap-8">
            <div />
            <div className="flex sm:justify-end">
              <Button type="submit" className="w-full sm:w-auto" disabled={saveMutation.isPending}>
                {saveMutation.isPending ? <Trans>Saving...</Trans> : <Trans>Save changes</Trans>}
              </Button>
            </div>
          </div>
        </Form>
      </AppLayout>

      <UnsavedChangesDialog
        isOpen={isConfirmDialogOpen}
        onConfirmLeave={confirmLeave}
        onCancel={cancelLeave}
        parentTrackingTitle="User profile"
      />
    </>
  );
}
