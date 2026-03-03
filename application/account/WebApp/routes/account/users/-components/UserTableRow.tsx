import { Trans } from "@lingui/react/macro";
import { Avatar, AvatarFallback, AvatarImage } from "@repo/ui/components/Avatar";
import { Badge } from "@repo/ui/components/Badge";
import { TableCell, TableRow } from "@repo/ui/components/Table";
import { getInitials } from "@repo/utils/string/getInitials";

import type { UserRole } from "@/shared/lib/api/client";

import { SmartDate } from "@/shared/components/SmartDate";
import { getUserRoleLabel } from "@/shared/lib/api/userRole";

import { DesktopUserActionMenu, MobileUserActionMenu } from "./UserActionMenus";

interface UserData {
  id: string;
  avatarUrl: string | null;
  firstName: string | null;
  lastName: string | null;
  email: string;
  title: string | null;
  role: string;
  emailConfirmed: boolean;
  createdAt: string;
  lastSeenAt: string | null;
}

interface UserTableRowProps<T extends UserData> {
  user: T;
  index: number;
  isSelected: boolean;
  isMobile: boolean;
  currentUserRole: string | undefined;
  currentUserId: string | undefined;
  onRowClick: (user: T, event: React.MouseEvent) => void;
  onSelectedUsersChange: (users: T[]) => void;
  onViewProfile: (user: T, isKeyboardOpen?: boolean) => void;
  onDeleteUser: (user: T) => void;
  onChangeRole: (user: T) => void;
}

export function UserTableRow<T extends UserData>({
  user,
  index,
  isSelected,
  isMobile,
  currentUserRole,
  currentUserId,
  onRowClick,
  onSelectedUsersChange,
  onViewProfile,
  onDeleteUser,
  onChangeRole
}: Readonly<UserTableRowProps<T>>) {
  const userRowContent = (
    <div className="flex h-14 w-full items-center justify-between gap-2 p-0">
      <div className="flex min-w-0 flex-1 items-center gap-2 text-left font-normal">
        <Avatar size="lg">
          <AvatarImage src={user.avatarUrl ?? undefined} />
          <AvatarFallback>
            {getInitials(user.firstName ?? undefined, user.lastName ?? undefined, user.email)}
          </AvatarFallback>
        </Avatar>
        <div className="flex min-w-0 flex-1 flex-col">
          <div className="flex items-center gap-2 truncate text-foreground">
            <span className="truncate">
              {user.firstName || user.lastName
                ? `${user.firstName} ${user.lastName}`.trim()
                : isMobile
                  ? user.email
                  : ""}
            </span>
            {!isMobile && !user.emailConfirmed && (
              <Badge variant="outline" className="shrink-0">
                <Trans>Pending</Trans>
              </Badge>
            )}
          </div>
          {isMobile && !user.emailConfirmed ? (
            <Badge variant="outline" className="mt-1 -ml-2 w-fit">
              <Trans>Pending</Trans>
            </Badge>
          ) : (
            <span className="block truncate text-sm text-muted-foreground">{user.title ?? ""}</span>
          )}
        </div>
      </div>
    </div>
  );

  const actionMenuProps = {
    user,
    currentUserRole,
    currentUserId,
    onSelectedUsersChange,
    onViewProfile,
    onDeleteUser,
    onChangeRole
  };

  return (
    <TableRow
      key={user.id}
      data-state={isSelected ? "selected" : undefined}
      className={`cursor-pointer select-none ${isSelected ? "bg-active-background hover:bg-active-background" : "hover:bg-hover-background"}`}
      onClick={(event) => onRowClick(user, event)}
      index={index}
    >
      <TableCell className="pr-8">
        {isMobile ? <MobileUserActionMenu {...actionMenuProps}>{userRowContent}</MobileUserActionMenu> : userRowContent}
      </TableCell>
      {!isMobile && (
        <>
          <TableCell>
            <span className="block h-full w-full justify-start truncate p-0 text-left font-normal">{user.email}</span>
          </TableCell>
          <TableCell>
            <SmartDate date={user.createdAt} className="text-foreground" />
          </TableCell>
          <TableCell>
            <SmartDate date={user.lastSeenAt} className="text-foreground" />
          </TableCell>
          <TableCell>
            <div className="flex h-full w-full items-center justify-between p-0">
              <Badge variant="outline">{getUserRoleLabel(user.role as UserRole)}</Badge>
              <DesktopUserActionMenu {...actionMenuProps} />
            </div>
          </TableCell>
        </>
      )}
    </TableRow>
  );
}
