import { Trans } from "@lingui/react/macro";
import { Avatar, AvatarFallback, AvatarImage } from "@repo/ui/components/Avatar";
import { Badge } from "@repo/ui/components/Badge";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { useFormatDate } from "@repo/ui/hooks/useSmartDate";
import { CalendarIcon, CheckCircle2Icon, HashIcon, MailIcon, XCircleIcon } from "lucide-react";

import type { components } from "@/shared/lib/api/client";

import { AbInclusionPinBadge } from "../../-shared/AbInclusionPinBadge";
import { UserActionsMenu } from "./UserActionsMenu";
import { getUserDisplayName, getUserInitials } from "./userDisplay";

type BackOfficeUserDetailResponse = components["schemas"]["BackOfficeUserDetailResponse"];

interface UserDetailHeaderProps {
  user: BackOfficeUserDetailResponse | undefined;
  userId: string;
  isLoading: boolean;
}

export function UserDetailHeader({ user, userId, isLoading }: Readonly<UserDetailHeaderProps>) {
  const formatDate = useFormatDate();

  return (
    <div className="flex items-center gap-4">
      {isLoading || !user ? (
        <>
          <Skeleton className="size-16 rounded-full" />
          <div className="flex min-w-0 flex-1 flex-col justify-center gap-1 self-center">
            <Skeleton className="h-7 w-64" />
            <Skeleton className="h-4 w-48" />
          </div>
        </>
      ) : (
        <>
          <Avatar className="size-16">
            {user.avatarUrl && (
              <AvatarImage src={user.avatarUrl} alt={getUserDisplayName(user.firstName, user.lastName, user.email)} />
            )}
            <AvatarFallback className="text-lg">
              {getUserInitials(user.firstName, user.lastName, user.email)}
            </AvatarFallback>
          </Avatar>
          <div className="flex min-w-0 flex-1 flex-col justify-center gap-1 self-center">
            <div className="flex flex-wrap items-center gap-2">
              <h1 className="m-0 min-w-0 truncate leading-tight">
                {getUserDisplayName(user.firstName, user.lastName, user.email)}
              </h1>
              {user.emailConfirmed ? (
                <Badge variant="outline" className="gap-1 border-emerald-500/30 text-emerald-600">
                  <CheckCircle2Icon className="size-3" />
                  <span className="hidden sm:inline">
                    <Trans>Email confirmed</Trans>
                  </span>
                </Badge>
              ) : (
                <Badge variant="outline" className="gap-1 border-amber-500/30 text-amber-700 dark:text-amber-300">
                  <XCircleIcon className="size-3" />
                  <span className="hidden sm:inline">
                    <Trans>Email pending</Trans>
                  </span>
                </Badge>
              )}
              <AbInclusionPinBadge pin={user.abInclusionPin} />
            </div>
            <div className="flex flex-wrap items-center gap-x-3 gap-y-1 text-sm text-muted-foreground">
              <span className="inline-flex items-center gap-1.5">
                <MailIcon className="size-3.5" aria-hidden={true} />
                <span>{user.email}</span>
              </span>
              <span className="inline-flex items-center gap-1.5">
                <CalendarIcon className="size-3.5" aria-hidden={true} />
                <Trans>
                  Created <span className="md:hidden">{formatDate(user.createdAt, false, false, true)}</span>
                  <span className="hidden md:inline">{formatDate(user.createdAt)}</span>
                </Trans>
              </span>
              <span className="inline-flex items-center gap-1.5 font-mono">
                <HashIcon className="size-3.5" aria-hidden={true} />
                <span>{userId}</span>
              </span>
            </div>
          </div>
          <UserActionsMenu
            userId={userId}
            userLabel={getUserDisplayName(user.firstName, user.lastName, user.email)}
            abInclusionPin={user.abInclusionPin}
          />
        </>
      )}
    </div>
  );
}
