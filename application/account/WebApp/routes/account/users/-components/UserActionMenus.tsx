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

interface UserActionMenuUser {
  id: string;
}

interface UserActionMenuProps<T extends UserActionMenuUser> {
  user: T;
  currentUserRole: string | undefined;
  currentUserId: string | undefined;
  onSelectedUsersChange: (users: T[]) => void;
  onViewProfile: (user: T, isKeyboardOpen?: boolean) => void;
  onDeleteUser: (user: T) => void;
  onChangeRole: (user: T) => void;
}

interface MobileUserActionMenuProps<T extends UserActionMenuUser> extends UserActionMenuProps<T> {
  children: React.ReactNode;
}

export function MobileUserActionMenu<T extends UserActionMenuUser>({
  user,
  currentUserRole,
  currentUserId,
  onSelectedUsersChange,
  onViewProfile,
  onDeleteUser,
  onChangeRole,
  children
}: Readonly<MobileUserActionMenuProps<T>>) {
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

export function DesktopUserActionMenu<T extends UserActionMenuUser>({
  user,
  currentUserRole,
  currentUserId,
  onSelectedUsersChange,
  onViewProfile,
  onDeleteUser,
  onChangeRole
}: Readonly<UserActionMenuProps<T>>) {
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
