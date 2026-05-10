import { Avatar, AvatarFallback, AvatarImage } from "@repo/ui/components/Avatar";
import { Badge } from "@repo/ui/components/Badge";
import { TableCell, TableRow } from "@repo/ui/components/Table";
import { MailIcon } from "lucide-react";

import type { components } from "@/shared/lib/api/client";

import { SmartDateTime } from "@/shared/components/SmartDateTime";
import { getUserRoleLabel } from "@/shared/lib/api/labels";

import { getUserDisplayName, getUserInitials } from "./userDisplay";

type BackOfficeUserSummary = components["schemas"]["BackOfficeUserSummary"];

export function UsersTableRow({
  user,
  formatDate
}: Readonly<{
  user: BackOfficeUserSummary;
  formatDate: (value: string | null | undefined, includeTime?: boolean, omitCurrentYear?: boolean) => string;
}>) {
  const displayName = getUserDisplayName(user.firstName, user.lastName, user.email);
  const initials = getUserInitials(user.firstName, user.lastName, user.email);

  return (
    <TableRow rowKey={user.id}>
      <TableCell>
        <div className="flex min-w-0 items-center gap-3">
          <Avatar size="default" className="size-10">
            {user.avatarUrl && <AvatarImage src={user.avatarUrl} alt={displayName} />}
            <AvatarFallback>{initials}</AvatarFallback>
          </Avatar>
          <div className="flex min-w-0 flex-col gap-0.5">
            <span className="truncate font-medium text-foreground">{displayName}</span>
            <span className="flex min-w-0 items-center gap-1.5 text-xs text-muted-foreground">
              <MailIcon className="size-3 shrink-0" aria-hidden={true} />
              <span className="truncate">{user.email}</span>
            </span>
          </div>
        </div>
      </TableCell>
      <TableCell className="hidden md:table-cell">
        <span className="truncate text-sm">{user.tenantName}</span>
      </TableCell>
      <TableCell className="hidden lg:table-cell">
        <Badge variant="outline">{getUserRoleLabel(user.role)}</Badge>
      </TableCell>
      <TableCell className="hidden lg:table-cell">
        {user.lastSeenAt ? (
          <div className="flex flex-col leading-tight">
            <SmartDateTime date={user.lastSeenAt} />
            <span className="text-xs text-muted-foreground tabular-nums">
              {formatDate(user.lastSeenAt, true, true)}
            </span>
          </div>
        ) : (
          <span className="text-muted-foreground">-</span>
        )}
      </TableCell>
      <TableCell className="hidden xl:table-cell">
        <div className="flex flex-col leading-tight">
          <SmartDateTime date={user.createdAt} />
          <span className="text-xs text-muted-foreground tabular-nums">{formatDate(user.createdAt, true, true)}</span>
        </div>
      </TableCell>
    </TableRow>
  );
}
