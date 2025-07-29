import { UserRole, api, type components } from "@/shared/lib/api/client";
import { getUserRoleLabel } from "@/shared/lib/api/userRole";
import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { AlertDialog } from "@repo/ui/components/AlertDialog";
import { Button } from "@repo/ui/components/Button";
import { DialogContent, DialogFooter } from "@repo/ui/components/DialogFooter";
import { Modal } from "@repo/ui/components/Modal";
import { Select, SelectItem } from "@repo/ui/components/Select";
import { toastQueue } from "@repo/ui/components/Toast";
import { useCallback, useState } from "react";

type UserDetails = components["schemas"]["UserDetails"];

interface ChangeUserRoleDialogProps {
  user: UserDetails | null;
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
}

export function ChangeUserRoleDialog({ user, isOpen, onOpenChange }: Readonly<ChangeUserRoleDialogProps>) {
  const [selectedRole, setSelectedRole] = useState<UserRole | null>(null);
  const changeUserRoleMutation = api.useMutation("put", "/api/account-management/users/{id}/change-user-role");

  const handleConfirm = useCallback(async () => {
    if (!user || !selectedRole) {
      return;
    }

    try {
      await changeUserRoleMutation.mutateAsync({
        params: { path: { id: user.id } },
        body: { userRole: selectedRole }
      });

      const userDisplayName = `${user.firstName ?? ""} ${user.lastName ?? ""}`.trim() || user.email;
      toastQueue.add({
        title: t`Success`,
        description: t`User role updated successfully for ${userDisplayName}`,
        variant: "success"
      });

      onOpenChange(false);
      setSelectedRole(null);
    } catch (_error) {
      // Error is handled by the mutation
    }
  }, [user, selectedRole, changeUserRoleMutation, onOpenChange]);

  const handleCancel = useCallback(() => {
    onOpenChange(false);
    setSelectedRole(null);
  }, [onOpenChange]);

  return (
    <Modal isOpen={isOpen} onOpenChange={onOpenChange} blur={false} isDismissable={true}>
      <AlertDialog title={t`Change user role`}>
        <div className="flex flex-col max-sm:h-full">
          <DialogContent>
            <p className="text-muted-foreground text-sm">
              <Trans>
                Select a new role for{" "}
                <b>{user ? `${user.firstName ?? ""} ${user.lastName ?? ""}`.trim() || user.email : ""}</b>
              </Trans>
            </p>

            <div className="mt-4">
              <Select
                autoFocus={true}
                aria-label={t`User role`}
                selectedKey={selectedRole || user?.role}
                onSelectionChange={(key) => setSelectedRole(key as UserRole)}
                className="flex w-full flex-col"
              >
                {Object.values(UserRole).map((userRole) => (
                  <SelectItem id={userRole} key={userRole}>
                    {getUserRoleLabel(userRole)}
                  </SelectItem>
                ))}
              </Select>
            </div>
          </DialogContent>

          <DialogFooter>
            <Button variant="outline" onPress={handleCancel}>
              {t`Cancel`}
            </Button>
            <Button variant="primary" onPress={handleConfirm} isDisabled={!selectedRole || selectedRole === user?.role}>
              {t`OK`}
            </Button>
          </DialogFooter>
        </div>
      </AlertDialog>
    </Modal>
  );
}
