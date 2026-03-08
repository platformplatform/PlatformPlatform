import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { trackInteraction } from "@repo/infrastructure/applicationInsights/ApplicationInsightsProvider";
import { Button } from "@repo/ui/components/Button";
import {
  ContextMenu,
  ContextMenuContent,
  ContextMenuItem,
  ContextMenuSeparator,
  ContextMenuTrigger
} from "@repo/ui/components/ContextMenu";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger
} from "@repo/ui/components/DropdownMenu";
import { EllipsisVerticalIcon, SettingsIcon, Trash2Icon, UserIcon } from "lucide-react";

import type { components } from "@/shared/lib/api/client";

type UserDetails = components["schemas"]["UserDetails"];

interface UserActionMenuProps {
  user: UserDetails;
  currentUserRole: string | undefined;
  currentUserId: string | undefined;
  onSelectedUsersChange: (users: UserDetails[]) => void;
  onViewProfile: (user: UserDetails, isKeyboardOpen?: boolean) => void;
  onDeleteUser: (user: UserDetails) => void;
  onChangeRole: (user: UserDetails) => void;
}

interface MobileUserActionMenuProps extends UserActionMenuProps {
  children: React.ReactNode;
}

export function MobileUserActionMenu({
  user,
  currentUserRole,
  currentUserId,
  onSelectedUsersChange,
  onViewProfile,
  onDeleteUser,
  onChangeRole,
  children
}: Readonly<MobileUserActionMenuProps>) {
  return (
    <ContextMenu
      onOpenChange={(isOpen) => {
        if (isOpen) {
          onSelectedUsersChange([user]);
          trackInteraction("User actions", "menu", "Open");
        }
      }}
    >
      <ContextMenuTrigger className="block w-full">{children}</ContextMenuTrigger>
      <ContextMenuContent className="w-auto">
        <ContextMenuItem onClick={() => onViewProfile(user, false)}>
          <UserIcon className="size-4" />
          <Trans>View profile</Trans>
        </ContextMenuItem>
        {currentUserRole === "Owner" && (
          <>
            <ContextMenuItem disabled={user.id === currentUserId} onClick={() => onChangeRole(user)}>
              <SettingsIcon className="size-4" />
              <Trans>Change role</Trans>
            </ContextMenuItem>
            <ContextMenuSeparator />
            <ContextMenuItem
              disabled={user.id === currentUserId}
              variant="destructive"
              onClick={() => onDeleteUser(user)}
            >
              <Trash2Icon className="size-4" />
              <Trans>Delete</Trans>
            </ContextMenuItem>
          </>
        )}
      </ContextMenuContent>
    </ContextMenu>
  );
}

export function DesktopUserActionMenu({
  user,
  currentUserRole,
  currentUserId,
  onSelectedUsersChange,
  onViewProfile,
  onDeleteUser,
  onChangeRole
}: Readonly<UserActionMenuProps>) {
  return (
    <DropdownMenu
      onOpenChange={(isOpen) => {
        if (isOpen) {
          onSelectedUsersChange([user]);
          trackInteraction("User actions", "menu", "Open");
        }
      }}
    >
      <DropdownMenuTrigger
        render={
          <Button variant="ghost" size="icon" tabIndex={-1} aria-label={t`User actions`}>
            <EllipsisVerticalIcon className="size-5 text-muted-foreground" />
          </Button>
        }
      />
      <DropdownMenuContent className="w-auto">
        <DropdownMenuItem onClick={() => onViewProfile(user, false)}>
          <UserIcon className="size-4" />
          <Trans>View profile</Trans>
        </DropdownMenuItem>
        {currentUserRole === "Owner" && (
          <>
            <DropdownMenuItem disabled={user.id === currentUserId} onClick={() => onChangeRole(user)}>
              <SettingsIcon className="size-4" />
              <Trans>Change role</Trans>
            </DropdownMenuItem>
            <DropdownMenuSeparator />
            <DropdownMenuItem
              disabled={user.id === currentUserId}
              variant="destructive"
              onClick={() => onDeleteUser(user)}
            >
              <Trash2Icon className="size-4" />
              <Trans>Delete</Trans>
            </DropdownMenuItem>
          </>
        )}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
