import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { AuthenticationContext } from "@repo/infrastructure/auth/AuthenticationProvider";
import { userCollection } from "@repo/infrastructure/sync/collections";
import { useUser } from "@repo/infrastructure/sync/hooks";
import { useElectricMutation } from "@repo/infrastructure/sync/useElectricMutation";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { Button } from "@repo/ui/components/Button";
import { Form } from "@repo/ui/components/Form";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { mutationSubmitter } from "@repo/ui/forms/mutationSubmitter";
import { useUnsavedChangesGuard } from "@repo/ui/hooks/useUnsavedChangesGuard";
import { createFileRoute } from "@tanstack/react-router";
import { useContext, useEffect, useState } from "react";
import { toast } from "sonner";

import { UnsavedChangesDialog } from "@/shared/components/UnsavedChangesDialog";
import { UserProfileFields } from "@/shared/components/UserProfileFields";
import { apiClient } from "@/shared/lib/api/client";

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

  const saveMutation = useElectricMutation({
    mutationFn: async (data: { body: { firstName: string; lastName: string; title: string } }) => {
      if (selectedAvatarFile) {
        const formData = new FormData();
        formData.append("file", selectedAvatarFile);
        const { error: avatarError } = await apiClient.POST("/api/account/users/me/update-avatar", {
          body: formData as unknown as { file: string | null }
        });
        if (avatarError) {
          throw avatarError;
        }
      } else if (removeAvatarFlag) {
        const { error: removeError } = await apiClient.DELETE("/api/account/users/me/remove-avatar");
        if (removeError) {
          throw removeError;
        }
      }

      const { error } = await apiClient.PUT("/api/account/users/me", data);
      if (error) {
        throw error;
      }
    },
    utils: userCollection.utils,
    onMutate: () => {
      if (userId) {
        userCollection.update(userId, (draft) => {
          draft.firstName = firstName || null;
          draft.lastName = lastName || null;
          draft.title = title || null;
        });
      }
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
        {user === undefined ? (
          <div className="mt-8 flex flex-col gap-6 md:grid md:grid-cols-[8.5rem_1fr] md:gap-8">
            <div className="flex flex-col">
              <Skeleton className="mb-2 h-5 w-24" />
              <div className="flex h-[8.5rem] w-full items-center justify-center md:size-[8.5rem]">
                <Skeleton className="size-[7rem] rounded-full" />
              </div>
            </div>
            <div className="flex flex-col gap-4">
              <div className="flex flex-col gap-4 sm:flex-row">
                <Skeleton className="h-16 sm:flex-1" />
                <Skeleton className="h-16 sm:flex-1" />
              </div>
              <Skeleton className="h-16 w-full" />
              <Skeleton className="h-16 w-full" />
            </div>
          </div>
        ) : (
          <Form
            onSubmit={mutationSubmitter(saveMutation)}
            validationBehavior="aria"
            validationErrors={saveMutation.validationErrors}
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
        )}
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
