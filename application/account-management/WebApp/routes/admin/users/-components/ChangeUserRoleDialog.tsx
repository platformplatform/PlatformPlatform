import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Avatar } from "@repo/ui/components/Avatar";
import { Button } from "@repo/ui/components/Button";
import { Dialog } from "@repo/ui/components/Dialog";
import { DialogContent, DialogFooter, DialogHeader } from "@repo/ui/components/DialogFooter";
import { Form } from "@repo/ui/components/Form";
import { Heading } from "@repo/ui/components/Heading";
import { Radio, RadioGroup } from "@repo/ui/components/RadioGroup";
import { Text } from "@repo/ui/components/Text";
import { toastQueue } from "@repo/ui/components/Toast";
import { getInitials } from "@repo/utils/string/getInitials";
import { useQueryClient } from "@tanstack/react-query";
import { XIcon } from "lucide-react";
import { useState } from "react";
import { DirtyModal } from "@/shared/components/DirtyModal";
import { api, type components, UserRole } from "@/shared/lib/api/client";

type UserDetails = components["schemas"]["UserDetails"];

interface ChangeUserRoleDialogProps {
  user: UserDetails | null;
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
}

export function ChangeUserRoleDialog({ user, isOpen, onOpenChange }: Readonly<ChangeUserRoleDialogProps>) {
  const queryClient = useQueryClient();
  const [selectedRole, setSelectedRole] = useState<UserRole | null>(null);

  const hasUnsavedChanges = selectedRole !== null && selectedRole !== user?.role;

  const changeUserRoleMutation = api.useMutation("put", "/api/account-management/users/{id}/change-user-role", {
    onSuccess: () => {
      if (!user) {
        return;
      }

      const userDisplayName = `${user.firstName ?? ""} ${user.lastName ?? ""}`.trim() || user.email;
      toastQueue.add({
        title: t`Success`,
        description: t`User role updated successfully for ${userDisplayName}`,
        variant: "success"
      });
      queryClient.invalidateQueries({
        queryKey: ["get", "/api/account-management/users"]
      });
      onOpenChange(false);
    }
  });

  const handleCloseComplete = () => {
    setSelectedRole(null);
  };

  const handleCancel = () => {
    setSelectedRole(null);
    onOpenChange(false);
  };

  if (!user) {
    return null;
  }

  const displayName = [user.firstName, user.lastName].filter(Boolean).join(" ") || user.email;
  const currentRole = selectedRole ?? user.role;

  const handleSubmit = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    changeUserRoleMutation.mutate({
      params: {
        path: { id: user.id }
      },
      body: { userRole: currentRole }
    });
  };

  return (
    <DirtyModal
      isOpen={isOpen}
      onOpenChange={onOpenChange}
      hasUnsavedChanges={hasUnsavedChanges}
      isDismissable={!changeUserRoleMutation.isPending}
      onCloseComplete={handleCloseComplete}
    >
      <Dialog className="sm:w-dialog-lg">
        {({ close }) => (
          <>
            <XIcon onClick={close} className="absolute top-2 right-2 h-10 w-10 cursor-pointer p-2 hover:bg-muted" />
            <DialogHeader>
              <Heading slot="title" className="text-2xl">
                <Trans>Change user role</Trans>
              </Heading>
            </DialogHeader>

            <Form
              onSubmit={handleSubmit}
              validationErrors={changeUserRoleMutation.error?.errors}
              validationBehavior="aria"
              className="flex flex-col max-sm:h-full"
            >
              <DialogContent className="flex flex-col gap-6">
                <div className="flex items-center gap-3">
                  <Avatar
                    initials={getInitials(user.firstName ?? undefined, user.lastName ?? undefined, user.email)}
                    avatarUrl={user.avatarUrl}
                    size="lg"
                    isRound={true}
                  />
                  <div className="min-w-0 flex-1">
                    <Text className="truncate font-medium">{displayName}</Text>
                    {user.title && <Text className="truncate text-muted-foreground text-sm">{user.title}</Text>}
                    <Text className="truncate text-muted-foreground text-sm">{user.email}</Text>
                  </div>
                </div>

                <RadioGroup
                  aria-label={t`Role`}
                  value={currentRole}
                  onChange={(value) => setSelectedRole(value as UserRole)}
                  orientation="vertical"
                >
                  <div className="flex flex-col gap-2 rounded-md border border-border p-3">
                    <Radio value={UserRole.Owner}>
                      <div className="flex flex-col">
                        <span className="font-medium">
                          <Trans>Owner</Trans>
                        </span>
                        <span className="text-muted-foreground text-sm">
                          <Trans>Full access including user roles and account settings</Trans>
                        </span>
                      </div>
                    </Radio>
                  </div>
                  <div className="flex flex-col gap-2 rounded-md border border-border p-3">
                    <Radio value={UserRole.Admin}>
                      <div className="flex flex-col">
                        <span className="font-medium">
                          <Trans>Admin</Trans>
                        </span>
                        <span className="text-muted-foreground text-sm">
                          <Trans>Full access except changing user roles and account settings</Trans>
                        </span>
                      </div>
                    </Radio>
                  </div>
                  <div className="flex flex-col gap-2 rounded-md border border-border p-3">
                    <Radio value={UserRole.Member}>
                      <div className="flex flex-col">
                        <span className="font-medium">
                          <Trans>Member</Trans>
                        </span>
                        <span className="text-muted-foreground text-sm">
                          <Trans>Standard user access</Trans>
                        </span>
                      </div>
                    </Radio>
                  </div>
                </RadioGroup>
              </DialogContent>
              <DialogFooter>
                <Button
                  type="reset"
                  onPress={handleCancel}
                  variant="secondary"
                  isDisabled={changeUserRoleMutation.isPending}
                >
                  <Trans>Cancel</Trans>
                </Button>
                <Button type="submit" isDisabled={changeUserRoleMutation.isPending}>
                  {changeUserRoleMutation.isPending ? <Trans>Saving...</Trans> : <Trans>Save changes</Trans>}
                </Button>
              </DialogFooter>
            </Form>
          </>
        )}
      </Dialog>
    </DirtyModal>
  );
}
