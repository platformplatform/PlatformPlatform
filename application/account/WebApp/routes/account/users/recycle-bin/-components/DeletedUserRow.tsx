import type { useDeletedUsers } from "@repo/infrastructure/sync/hooks";

import { t } from "@lingui/core/macro";
import { Avatar, AvatarFallback, AvatarImage } from "@repo/ui/components/Avatar";
import { Badge } from "@repo/ui/components/Badge";
import { Checkbox } from "@repo/ui/components/Checkbox";
import { TableCell, TableRow } from "@repo/ui/components/Table";
import { isMediumViewportOrLarger, isSmallViewportOrLarger } from "@repo/ui/utils/responsive";
import { getInitials } from "@repo/utils/string/getInitials";

import type { UserRole } from "@/shared/lib/api/client";

import { SmartDate } from "@/shared/components/SmartDate";
import { getUserRoleLabel } from "@/shared/lib/api/userRole";

type ElectricDeletedUser = ReturnType<typeof useDeletedUsers>["data"][number];

interface DeletedUserRowProps {
  user: ElectricDeletedUser;
  isSelected: boolean;
  isMultiSelectMode: boolean;
  onSelectRow: (user: ElectricDeletedUser, isCheckboxClick: boolean) => void;
  onRowClick: (user: ElectricDeletedUser, event: React.MouseEvent) => void;
}

export function DeletedUserRow({
  user,
  isSelected,
  isMultiSelectMode,
  onSelectRow,
  onRowClick
}: Readonly<DeletedUserRowProps>) {
  return (
    <TableRow
      key={user.id}
      data-state={isSelected ? "selected" : undefined}
      className={`cursor-pointer select-none ${isSelected ? "bg-active-background hover:bg-active-background" : "hover:bg-hover-background"}`}
      onClick={(event) => onRowClick(user, event)}
    >
      {isMultiSelectMode && (
        <TableCell>
          <Checkbox
            checked={isSelected}
            onCheckedChange={() => onSelectRow(user, true)}
            aria-label={t`Select ${user.firstName ?? ""} ${user.lastName ?? ""}`}
          />
        </TableCell>
      )}
      <TableCell>
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
                    : !isSmallViewportOrLarger()
                      ? user.email
                      : ""}
                </span>
              </div>
              <span className="block truncate text-sm text-muted-foreground">{user.title ?? ""}</span>
            </div>
          </div>
        </div>
      </TableCell>
      {isSmallViewportOrLarger() && (
        <TableCell>
          <span className="block h-full w-full justify-start p-0 text-left font-normal">{user.email}</span>
        </TableCell>
      )}
      {isMediumViewportOrLarger() && (
        <TableCell>
          <SmartDate date={user.deletedAt} className="text-foreground" />
        </TableCell>
      )}
      {isSmallViewportOrLarger() && (
        <TableCell>
          <Badge variant="outline">{getUserRoleLabel(user.role as UserRole)}</Badge>
        </TableCell>
      )}
    </TableRow>
  );
}
