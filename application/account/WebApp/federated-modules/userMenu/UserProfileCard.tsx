import type { useUserInfo } from "@repo/infrastructure/auth/hooks";

import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Avatar, AvatarFallback, AvatarImage } from "@repo/ui/components/Avatar";
import { DropdownMenuGroup, DropdownMenuItem } from "@repo/ui/components/DropdownMenu";
import { PencilIcon } from "lucide-react";
import { useState } from "react";

interface UserProfileCardProps {
  userInfo: NonNullable<ReturnType<typeof useUserInfo>>;
  onNavigateToProfile: () => void;
}

export function UserProfileCard({ userInfo, onNavigateToProfile }: Readonly<UserProfileCardProps>) {
  const [isHighlighted, setIsHighlighted] = useState(false);

  return (
    <DropdownMenuGroup>
      <DropdownMenuItem
        onClick={onNavigateToProfile}
        className="flex flex-col items-center gap-1 px-4 py-3"
        aria-label={t`Edit user profile`}
        onMouseEnter={() => setIsHighlighted(true)}
        onMouseLeave={() => setIsHighlighted(false)}
      >
        <div className="relative">
          <Avatar className="size-16">
            <AvatarImage src={userInfo.avatarUrl ?? undefined} />
            <AvatarFallback className="text-xl">{userInfo.initials ?? ""}</AvatarFallback>
          </Avatar>
          <div
            className="pointer-events-none absolute -right-0.5 -bottom-0.5 flex size-5 items-center justify-center rounded-full border border-border [&_*]:!text-inherit"
            style={{
              backgroundColor: isHighlighted ? "var(--color-primary)" : "var(--color-popover)",
              color: isHighlighted ? "var(--color-primary-foreground)" : "var(--color-muted-foreground)"
            }}
          >
            <PencilIcon className="size-2.5" strokeWidth={3} />
          </div>
        </div>
        <span className="font-medium">{userInfo.fullName}</span>
        <span className="text-sm text-muted-foreground group-focus/dropdown-menu-item:hidden">{userInfo.email}</span>
        <span className="hidden text-sm group-focus/dropdown-menu-item:inline">
          <Trans>Edit profile</Trans>
        </span>
      </DropdownMenuItem>
    </DropdownMenuGroup>
  );
}
