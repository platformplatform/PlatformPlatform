import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { trackInteraction } from "@repo/infrastructure/applicationInsights/ApplicationInsightsProvider";
import { userCollection } from "@repo/infrastructure/sync/collections";
import { Avatar, AvatarFallback, AvatarImage } from "@repo/ui/components/Avatar";
import { Button } from "@repo/ui/components/Button";
import {
  DialogBody,
  DialogClose,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle
} from "@repo/ui/components/Dialog";
import { DirtyDialog } from "@repo/ui/components/DirtyDialog";
import { useDialogSetDirty } from "@repo/ui/components/DirtyDialogContext";
import { Field, FieldContent, FieldDescription, FieldLabel, FieldTitle } from "@repo/ui/components/Field";
import { Form, type FormProps } from "@repo/ui/components/Form";
import { Item, ItemContent, ItemDescription, ItemMedia, ItemTitle } from "@repo/ui/components/Item";
import { RadioGroup, RadioGroupItem } from "@repo/ui/components/RadioGroup";
import { getInitials } from "@repo/utils/string/getInitials";
import { useState } from "react";
import { toast } from "sonner";

import { api, UserRole } from "@/shared/lib/api/client";

interface UserData {
  id: string;
  avatarUrl: string | null;
  firstName: string | null;
  lastName: string | null;
  email: string;
  title: string | null;
  role: string;
}

interface ChangeUserRoleDialogProps {
  user: UserData | null;
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
}

export function ChangeUserRoleDialog({ user, isOpen, onOpenChange }: Readonly<ChangeUserRoleDialogProps>) {
  if (!user) {
    return null;
  }

  const handleClose = () => onOpenChange(false);

  return (
    <DirtyDialog open={isOpen} onOpenChange={onOpenChange} trackingTitle="Change user role">
      <DialogContent className="sm:w-dialog-lg">
        <DialogHeader>
          <DialogTitle>
            <Trans>Change user role</Trans>
          </DialogTitle>
        </DialogHeader>
        <ChangeUserRoleDialogBody user={user} onClose={handleClose} />
      </DialogContent>
    </DirtyDialog>
  );
}

function ChangeUserRoleDialogBody({ user, onClose }: { user: UserData; onClose: () => void }) {
  const setDirty = useDialogSetDirty();
  const [selectedRole, setSelectedRole] = useState<UserRole | null>(null);

  const changeUserRoleMutation = api.useMutation("put", "/api/account/users/{id}/change-user-role", {
    meta: { skipQueryInvalidation: true },
    onSuccess: () => {
      userCollection.update(user.id, (draft) => {
        draft.role = selectedRole ?? user.role;
      });
      const userDisplayName = `${user.firstName ?? ""} ${user.lastName ?? ""}`.trim() || user.email;
      toast.success(t`User role updated successfully for ${userDisplayName}`);
      onClose();
    }
  });

  const displayName = [user.firstName, user.lastName].filter(Boolean).join(" ") || user.email;
  const currentRole = selectedRole ?? user.role;

  const handleSubmit: NonNullable<FormProps["onSubmit"]> = (event) => {
    event.preventDefault();
    changeUserRoleMutation.mutate({
      params: { path: { id: user.id } },
      body: { userRole: currentRole as UserRole }
    });
  };

  return (
    <Form
      onSubmit={handleSubmit}
      validationErrors={changeUserRoleMutation.error?.errors}
      validationBehavior="aria"
      className="flex flex-col max-sm:h-full"
    >
      <DialogBody>
        <Item className="p-0">
          <ItemMedia variant="image" className="size-16">
            <Avatar className="size-16">
              <AvatarImage src={user.avatarUrl ?? undefined} />
              <AvatarFallback>
                {getInitials(user.firstName ?? undefined, user.lastName ?? undefined, user.email)}
              </AvatarFallback>
            </Avatar>
          </ItemMedia>
          <ItemContent>
            <ItemTitle className="truncate font-medium">{displayName}</ItemTitle>
            {user.title && <ItemDescription className="truncate">{user.title}</ItemDescription>}
            <ItemDescription className="truncate">{user.email}</ItemDescription>
          </ItemContent>
        </Item>

        <RadioGroup
          aria-label={t`Role`}
          value={currentRole}
          onValueChange={(value) => {
            trackInteraction("Change user role", "interaction", "Select role");
            setSelectedRole(value as UserRole);
            setDirty(true);
          }}
          className="mt-3"
        >
          <FieldLabel>
            <Field orientation="horizontal">
              <RadioGroupItem value={UserRole.Owner} id="role-owner" aria-label={t`Owner`} autoFocus={true} />
              <FieldContent>
                <FieldTitle>
                  <Trans>Owner</Trans>
                </FieldTitle>
                <FieldDescription>
                  <Trans>Full access including user roles and account settings</Trans>
                </FieldDescription>
              </FieldContent>
            </Field>
          </FieldLabel>
          <FieldLabel>
            <Field orientation="horizontal">
              <RadioGroupItem value={UserRole.Admin} id="role-admin" aria-label={t`Admin`} />
              <FieldContent>
                <FieldTitle>
                  <Trans>Admin</Trans>
                </FieldTitle>
                <FieldDescription>
                  <Trans>Full access except changing user roles and account settings</Trans>
                </FieldDescription>
              </FieldContent>
            </Field>
          </FieldLabel>
          <FieldLabel>
            <Field orientation="horizontal">
              <RadioGroupItem value={UserRole.Member} id="role-member" aria-label={t`Member`} />
              <FieldContent>
                <FieldTitle>
                  <Trans>Member</Trans>
                </FieldTitle>
                <FieldDescription>
                  <Trans>Standard user access</Trans>
                </FieldDescription>
              </FieldContent>
            </Field>
          </FieldLabel>
        </RadioGroup>
      </DialogBody>
      <DialogFooter>
        <DialogClose render={<Button type="reset" variant="secondary" disabled={changeUserRoleMutation.isPending} />}>
          <Trans>Cancel</Trans>
        </DialogClose>
        <Button type="submit" disabled={changeUserRoleMutation.isPending}>
          {changeUserRoleMutation.isPending ? <Trans>Saving...</Trans> : <Trans>Save changes</Trans>}
        </Button>
      </DialogFooter>
    </Form>
  );
}
