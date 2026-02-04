import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { AuthenticationContext } from "@repo/infrastructure/auth/AuthenticationProvider";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { Button } from "@repo/ui/components/Button";
import { Form } from "@repo/ui/components/Form";
import { mutationSubmitter } from "@repo/ui/forms/mutationSubmitter";
import { useUnsavedChangesGuard } from "@repo/ui/hooks/useUnsavedChangesGuard";
import type { FileUploadMutation } from "@repo/ui/types/FileUpload";
import { useMutation } from "@tanstack/react-query";
import { createFileRoute } from "@tanstack/react-router";
import { useContext, useState } from "react";
import { toast } from "sonner";
import { UnsavedChangesDialog } from "@/shared/components/UnsavedChangesDialog";
import { UserProfileFields } from "@/shared/components/UserProfileFields";
import { api, type Schemas } from "@/shared/lib/api/client";

export const Route = createFileRoute("/account/profile/")({
  component: ProfilePage
});

function ProfilePage() {
  const [selectedAvatarFile, setSelectedAvatarFile] = useState<File | null>(null);
  const [removeAvatarFlag, setRemoveAvatarFlag] = useState(false);
  const [isFormDirty, setIsFormDirty] = useState(false);

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
      }

      await updateCurrentUserMutation.mutateAsync(data);

      const { data: updatedUser } = await refetch();
      if (updatedUser) {
        updateUserInfo(updatedUser);
      }
    },
    onSuccess: () => {
      setSelectedAvatarFile(null);
      setRemoveAvatarFlag(false);
      setIsFormDirty(false);
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

  if (isLoadingUser) {
    return (
      <AppLayout variant="center">
        <div className="flex flex-1 items-center justify-center">
          <Trans>Loading...</Trans>
        </div>
      </AppLayout>
    );
  }

  if (!user) {
    return (
      <AppLayout variant="center">
        <div className="flex flex-1 items-center justify-center">
          <Trans>Unable to load profile</Trans>
        </div>
      </AppLayout>
    );
  }

  return (
    <>
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
          <UserProfileFields
            user={user}
            isPending={saveMutation.isPending}
            onAvatarFileSelect={handleAvatarFileSelect}
            onAvatarRemove={handleAvatarRemove}
          />

          <div className="mt-4 flex justify-end">
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
