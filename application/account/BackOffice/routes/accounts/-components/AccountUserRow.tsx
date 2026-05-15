import { Trans } from "@lingui/react/macro";
import { Avatar, AvatarFallback, AvatarImage } from "@repo/ui/components/Avatar";
import { Badge } from "@repo/ui/components/Badge";
import { TableCell, TableRow } from "@repo/ui/components/Table";
import { getInitials } from "@repo/utils/string/getInitials";
import { MailIcon } from "lucide-react";

import type { components } from "@/shared/lib/api/client";

import { getUserRoleLabel } from "@/shared/lib/api/labels";

type TenantUserSummary = components["schemas"]["TenantUserSummary"];

export function AccountUserRow({
  user,
  formatDate
}: Readonly<{
  user: TenantUserSummary;
  formatDate: (value: string | null | undefined) => string;
}>) {
  const displayName =
    user.firstName || user.lastName ? `${user.firstName ?? ""} ${user.lastName ?? ""}`.trim() : user.email;

  return (
    <TableRow rowKey={user.id}>
      <TableCell>
        <div className="flex items-center gap-2">
          <Avatar size="lg">
            <AvatarImage src={user.avatarUrl ?? undefined} alt="" />
            <AvatarFallback>
              {getInitials(user.firstName ?? undefined, user.lastName ?? undefined, user.email)}
            </AvatarFallback>
          </Avatar>
          <div className="flex min-w-0 flex-col leading-tight">
            <span className="truncate text-sm font-medium">{displayName}</span>
            {user.title && <span className="truncate text-xs text-muted-foreground">{user.title}</span>}
            <span className="flex min-w-0 items-center gap-1.5 text-xs text-muted-foreground md:hidden">
              <MailIcon className="size-3 shrink-0" aria-hidden={true} />
              <span className="truncate">{user.email}</span>
            </span>
          </div>
          {!user.emailConfirmed && (
            <Badge variant="outline" className="shrink-0">
              <Trans>Pending</Trans>
            </Badge>
          )}
        </div>
      </TableCell>
      <TableCell className="hidden md:table-cell">
        <span className="flex min-w-0 items-center gap-1.5">
          <MailIcon className="size-3.5 shrink-0 text-muted-foreground" aria-hidden={true} />
          <span className="truncate">{user.email}</span>
        </span>
      </TableCell>
      <TableCell className="hidden lg:table-cell">
        <Badge variant="outline">{getUserRoleLabel(user.role)}</Badge>
      </TableCell>
      <TableCell className="hidden lg:table-cell">{user.lastSeenAt ? formatDate(user.lastSeenAt) : "-"}</TableCell>
    </TableRow>
  );
}
