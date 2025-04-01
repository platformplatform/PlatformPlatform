import { UserRole, api, type components } from "@/shared/lib/api/client";
import { getUserRoleLabel } from "@/shared/lib/api/userRole";
import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { AlertDialog } from "@repo/ui/components/AlertDialog";
import { Modal } from "@repo/ui/components/Modal";
import { Select, SelectItem } from "@repo/ui/components/Select";
import { useCallback } from "react";

type UserDetails = components["schemas"]["UserDetails"];

interface ChangeUserRoleDialogProps {
  user: UserDetails | null;
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
}

export function ChangeUserRoleDialog({ user, isOpen, onOpenChange }: Readonly<ChangeUserRoleDialogProps>) {
  const changeUserRoleMutation = api.useMutation("put", "/api/account-management/users/{id}/change-user-role");

  const handleUserRoleChange = useCallback(
    async (newUserRole: UserRole) => {
      if (!user) {
        return null;
      }

      await changeUserRoleMutation.mutateAsync({ params: { path: { id: user.id } }, body: { userRole: newUserRole } });

      onOpenChange(false);
    },
    [user, changeUserRoleMutation, onOpenChange]
  );

  return (
    <Modal isOpen={isOpen} onOpenChange={onOpenChange} blur={false} isDismissable={true}>
      <AlertDialog title={t`Change user role`}>
        <p className="text-muted-foreground text-sm">
          <Trans>
            Select a new role for{" "}
            <b>{user ? `${user.firstName ?? ""} ${user.lastName ?? ""}`.trim() || user.email : ""}</b>
          </Trans>
        </p>

        <div className="mt-4 flex flex-col gap-4">
          <Select
            autoFocus={true}
            aria-label={t`User role`}
            selectedKey={user?.role}
            onSelectionChange={(key) => handleUserRoleChange(key as UserRole)}
            className="flex w-full flex-col"
          >
            {Object.values(UserRole).map((userRole) => (
              <SelectItem id={userRole} key={userRole}>
                {getUserRoleLabel(userRole)}
              </SelectItem>
            ))}
          </Select>
        </div>
      </AlertDialog>
    </Modal>
  );
}
