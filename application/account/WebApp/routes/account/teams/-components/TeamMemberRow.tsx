import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Avatar, AvatarFallback, AvatarImage } from "@repo/ui/components/Avatar";
import { Badge } from "@repo/ui/components/Badge";
import { Button } from "@repo/ui/components/Button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger
} from "@repo/ui/components/DropdownMenu";
import { Item, ItemActions, ItemContent, ItemDescription, ItemMedia, ItemTitle } from "@repo/ui/components/Item";
import { getInitials } from "@repo/utils/string/getInitials";
import { EllipsisVerticalIcon } from "lucide-react";

import { type Schemas, TeamMemberRole } from "@/shared/lib/api/client";

type TeamMember = Schemas["TeamMemberDetails"];

interface TeamMemberRowProps {
  member: Pick<TeamMember, "userId" | "email" | "firstName" | "lastName" | "avatarUrl" | "role" | "title">;
  onChangeRole?: (userId: string, role: TeamMemberRole) => void;
}

export function TeamMemberRow({ member, onChangeRole }: Readonly<TeamMemberRowProps>) {
  const displayName = `${member.firstName ?? ""} ${member.lastName ?? ""}`.trim() || member.email;
  const isAdmin = member.role === TeamMemberRole.Admin;
  return (
    <Item size="sm" className="hover:bg-hover-background">
      <ItemMedia variant="image" className="size-10">
        <Avatar size="lg">
          <AvatarImage src={member.avatarUrl ?? undefined} />
          <AvatarFallback>
            {getInitials(member.firstName ?? undefined, member.lastName ?? undefined, member.email)}
          </AvatarFallback>
        </Avatar>
      </ItemMedia>
      <ItemContent>
        <ItemTitle>
          <span className="truncate font-medium text-foreground">{displayName}</span>
          {isAdmin && (
            <Badge variant="outline" className="shrink-0">
              <Trans>Admin</Trans>
            </Badge>
          )}
        </ItemTitle>
        <ItemDescription>{member.title ?? member.email}</ItemDescription>
      </ItemContent>
      {onChangeRole && (
        <ItemActions>
          <DropdownMenu>
            <DropdownMenuTrigger
              render={
                <Button variant="ghost" size="icon" aria-label={t`Member actions`}>
                  <EllipsisVerticalIcon className="size-4 text-muted-foreground" />
                </Button>
              }
            />
            <DropdownMenuContent className="w-auto">
              {isAdmin ? (
                <DropdownMenuItem onClick={() => onChangeRole(member.userId, TeamMemberRole.Member)}>
                  <Trans>Make member</Trans>
                </DropdownMenuItem>
              ) : (
                <DropdownMenuItem onClick={() => onChangeRole(member.userId, TeamMemberRole.Admin)}>
                  <Trans>Make admin</Trans>
                </DropdownMenuItem>
              )}
            </DropdownMenuContent>
          </DropdownMenu>
        </ItemActions>
      )}
    </Item>
  );
}
