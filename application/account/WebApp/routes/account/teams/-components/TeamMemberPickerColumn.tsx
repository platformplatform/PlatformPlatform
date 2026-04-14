import type { KeyboardEvent, MouseEvent, ReactNode } from "react";

import { Trans } from "@lingui/react/macro";
import { Avatar, AvatarFallback, AvatarImage } from "@repo/ui/components/Avatar";
import { Badge } from "@repo/ui/components/Badge";
import { Empty, EmptyDescription, EmptyHeader, EmptyMedia, EmptyTitle } from "@repo/ui/components/Empty";
import { getInitials } from "@repo/utils/string/getInitials";
import { UsersIcon } from "lucide-react";

import { TeamMemberRole } from "@/shared/lib/api/client";

import type { PickerUser } from "./teamMemberPickerUtils";

interface TeamMemberPickerColumnProps {
  title: ReactNode;
  count: number;
  emptyTitle: ReactNode;
  emptyDescription: ReactNode;
  users: PickerUser[];
  selectedIds: Set<string>;
  onActivate: (event: MouseEvent | KeyboardEvent, userId: string) => void;
  onDoubleActivate?: (userId: string) => void;
  showAdminBadge?: boolean;
}

export function TeamMemberPickerColumn({
  title,
  count,
  emptyTitle,
  emptyDescription,
  users,
  selectedIds,
  onActivate,
  onDoubleActivate,
  showAdminBadge = false
}: Readonly<TeamMemberPickerColumnProps>) {
  return (
    <div className="flex h-[16rem] min-h-0 min-w-0 flex-1 flex-col rounded-md border border-border sm:h-auto">
      <div className="flex items-center justify-between border-b border-border bg-muted/40 px-4 py-2">
        <span className="text-sm font-medium">{title}</span>
        <Badge variant="secondary">{count}</Badge>
      </div>
      <div
        className="flex-1 overflow-y-auto p-2"
        role="listbox"
        aria-multiselectable={true}
        tabIndex={users.length === 0 ? -1 : 0}
      >
        {users.length === 0 ? (
          <Empty className="h-full">
            <EmptyHeader>
              <EmptyMedia variant="icon">
                <UsersIcon />
              </EmptyMedia>
              <EmptyTitle>{emptyTitle}</EmptyTitle>
              <EmptyDescription>{emptyDescription}</EmptyDescription>
            </EmptyHeader>
          </Empty>
        ) : (
          <div className="flex flex-col gap-1">
            {users.map((user) => (
              <TeamMemberPickerRow
                key={user.userId}
                user={user}
                isSelected={selectedIds.has(user.userId)}
                onActivate={onActivate}
                onDoubleActivate={onDoubleActivate}
                showAdminBadge={showAdminBadge}
              />
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

interface TeamMemberPickerRowProps {
  user: PickerUser;
  isSelected: boolean;
  onActivate: (event: MouseEvent | KeyboardEvent, userId: string) => void;
  onDoubleActivate?: (userId: string) => void;
  showAdminBadge: boolean;
}

function TeamMemberPickerRow({
  user,
  isSelected,
  onActivate,
  onDoubleActivate,
  showAdminBadge
}: Readonly<TeamMemberPickerRowProps>) {
  const displayName = `${user.firstName ?? ""} ${user.lastName ?? ""}`.trim() || user.email;
  return (
    <div
      role="option"
      aria-selected={isSelected}
      tabIndex={0}
      onClick={(event) => onActivate(event, user.userId)}
      onDoubleClick={() => onDoubleActivate?.(user.userId)}
      onKeyDown={(event) => {
        if (event.key === "Enter" || event.key === " ") {
          event.preventDefault();
          onActivate(event, user.userId);
        }
      }}
      className={`flex cursor-pointer items-center gap-3 rounded-md border border-transparent p-2 outline-ring transition-colors hover:bg-hover-background focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 active:bg-accent ${
        isSelected ? "border-primary/40 bg-primary/10 hover:bg-primary/15" : ""
      }`}
    >
      <Avatar size="lg">
        <AvatarImage src={user.avatarUrl ?? undefined} />
        <AvatarFallback>
          {getInitials(user.firstName ?? undefined, user.lastName ?? undefined, user.email)}
        </AvatarFallback>
      </Avatar>
      <div className="min-w-0 flex-1">
        <div className="flex items-center gap-2">
          <span className="truncate font-medium text-foreground">{displayName}</span>
          {showAdminBadge && user.role === TeamMemberRole.Admin && (
            <Badge variant="outline" className="shrink-0">
              <Trans>Admin</Trans>
            </Badge>
          )}
        </div>
        {user.title && <div className="truncate text-sm text-muted-foreground">{user.title}</div>}
        <div className="truncate text-xs text-muted-foreground">{user.email}</div>
      </div>
    </div>
  );
}
