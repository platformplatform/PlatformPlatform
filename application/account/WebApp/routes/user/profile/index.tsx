import type { FileUploadMutation } from "@repo/ui/types/FileUpload";

import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { AuthenticationContext } from "@repo/infrastructure/auth/AuthenticationProvider";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { Button } from "@repo/ui/components/Button";
import { Form } from "@repo/ui/components/Form";
import { mutationSubmitter } from "@repo/ui/forms/mutationSubmitter";
import { useUnsavedChangesGuard } from "@repo/ui/hooks/useUnsavedChangesGuard";
import { useMutation } from "@tanstack/react-query";
import { createFileRoute } from "@tanstack/react-router";
import { HashIcon } from "lucide-react";
import { useContext, useState } from "react";
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
      <AppLayout variant="center" maxWidth="64rem">
        <div className="flex flex-1 items-center justify-center">
          <Trans>Loading...</Trans>
        </div>
      </AppLayout>
    );
  }

  if (!user) {
    return (
      <AppLayout variant="center" maxWidth="64rem">
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
            infoFields={
              <div className="flex flex-col gap-1 text-sm">
                <span className="text-muted-foreground">
                  <Trans>User ID</Trans>
                </span>
                <span className="inline-flex items-center gap-1.5 font-mono">
                  <HashIcon className="size-3.5" aria-hidden={true} />
                  {user.id}
                </span>
              </div>
            }
          />

          <div className="mt-4 md:grid md:grid-cols-[8.5rem_1fr] md:gap-8">
            <div />
            <div className="flex sm:justify-end">
              <Button type="submit" isPending={saveMutation.isPending}>
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
