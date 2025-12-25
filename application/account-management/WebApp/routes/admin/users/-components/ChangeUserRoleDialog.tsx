import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Avatar } from "@repo/ui/components/Avatar";
import { Button } from "@repo/ui/components/Button";
import { DialogClose, DialogContent, DialogFooter, DialogHeader, DialogTitle } from "@repo/ui/components/Dialog";
import { DirtyDialog } from "@repo/ui/components/DirtyDialog";
import { Form } from "@repo/ui/components/Form";
import { Label } from "@repo/ui/components/Label";
import { RadioGroup, RadioGroupItem } from "@repo/ui/components/RadioGroup";
import { Text } from "@repo/ui/components/Text";
import { getInitials } from "@repo/utils/string/getInitials";
import { useQueryClient } from "@tanstack/react-query";
import { useCallback, useState } from "react";
import { toast } from "sonner";
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

  const changeUserRoleMutation = api.useMutation("put", "/api/account-management/users/{id}/change-user-role", {
    onSuccess: () => {
      if (!user) {
        return;
      }

      setSelectedRole(null);
      const userDisplayName = `${user.firstName ?? ""} ${user.lastName ?? ""}`.trim() || user.email;
      toast.success(t`Success`, {
        description: t`User role updated successfully for ${userDisplayName}`
      });
      queryClient.invalidateQueries({
        queryKey: ["get", "/api/account-management/users"]
      });
      onOpenChange(false);
    }
  });

  const handleCloseComplete = useCallback(() => {
    setSelectedRole(null);
  }, []);

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
    <DirtyDialog
      open={isOpen}
      onOpenChange={onOpenChange}
      hasUnsavedChanges={selectedRole !== null}
      unsavedChangesTitle={t`Unsaved changes`}
      unsavedChangesMessage={<Trans>You have unsaved changes. If you leave now, your changes will be lost.</Trans>}
      leaveLabel={t`Leave`}
      stayLabel={t`Stay`}
      onCloseComplete={handleCloseComplete}
    >
      <DialogContent className="sm:w-dialog-lg">
        <DialogHeader>
          <DialogTitle>
            <Trans>Change user role</Trans>
          </DialogTitle>
        </DialogHeader>

        <Form
          onSubmit={handleSubmit}
          validationErrors={changeUserRoleMutation.error?.errors}
          validationBehavior="aria"
          className="flex flex-col max-sm:h-full"
        >
          <div className="flex flex-col gap-6">
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
              onValueChange={(value) => setSelectedRole(value as UserRole)}
            >
              <div className="flex flex-col gap-2 rounded-md border border-border p-3">
                <Label htmlFor="role-owner" className="flex cursor-pointer items-start gap-3">
                  <RadioGroupItem value={UserRole.Owner} id="role-owner" aria-label={t`Owner`} className="mt-0.5" />
                  <div className="flex flex-col">
                    <span className="font-medium">
                      <Trans>Owner</Trans>
                    </span>
                    <span className="font-normal text-muted-foreground text-sm">
                      <Trans>Full access including user roles and account settings</Trans>
                    </span>
                  </div>
                </Label>
              </div>
              <div className="flex flex-col gap-2 rounded-md border border-border p-3">
                <Label htmlFor="role-admin" className="flex cursor-pointer items-start gap-3">
                  <RadioGroupItem value={UserRole.Admin} id="role-admin" aria-label={t`Admin`} className="mt-0.5" />
                  <div className="flex flex-col">
                    <span className="font-medium">
                      <Trans>Admin</Trans>
                    </span>
                    <span className="font-normal text-muted-foreground text-sm">
                      <Trans>Full access except changing user roles and account settings</Trans>
                    </span>
                  </div>
                </Label>
              </div>
              <div className="flex flex-col gap-2 rounded-md border border-border p-3">
                <Label htmlFor="role-member" className="flex cursor-pointer items-start gap-3">
                  <RadioGroupItem value={UserRole.Member} id="role-member" aria-label={t`Member`} className="mt-0.5" />
                  <div className="flex flex-col">
                    <span className="font-medium">
                      <Trans>Member</Trans>
                    </span>
                    <span className="font-normal text-muted-foreground text-sm">
                      <Trans>Standard user access</Trans>
                    </span>
                  </div>
                </Label>
              </div>
            </RadioGroup>
          </div>
          <DialogFooter>
            <DialogClose
              render={<Button type="reset" variant="secondary" disabled={changeUserRoleMutation.isPending} />}
            >
              <Trans>Cancel</Trans>
            </DialogClose>
            <Button type="submit" disabled={changeUserRoleMutation.isPending}>
              {changeUserRoleMutation.isPending ? <Trans>Saving...</Trans> : <Trans>Save changes</Trans>}
            </Button>
          </DialogFooter>
        </Form>
      </DialogContent>
    </DirtyDialog>
  );
}
