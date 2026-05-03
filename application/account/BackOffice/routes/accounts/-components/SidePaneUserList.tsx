import { Avatar, AvatarFallback, AvatarImage } from "@repo/ui/components/Avatar";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { getInitials } from "@repo/utils/string/getInitials";

import type { components } from "@/shared/lib/api/client";

type TenantUserSummary = components["schemas"]["TenantUserSummary"];

export function SidePaneUserList({
  users,
  isLoading,
  emptyMessage
}: Readonly<{
  users: TenantUserSummary[];
  isLoading: boolean;
  emptyMessage: string;
}>) {
  if (isLoading) {
    return (
      <div className="flex flex-col gap-2">
        <Skeleton className="h-10 w-full" />
        <Skeleton className="h-10 w-full" />
      </div>
    );
  }
  if (users.length === 0) {
    return <span className="text-sm text-muted-foreground">{emptyMessage}</span>;
  }
  return (
    <div className="flex flex-col gap-2">
      {users.map((user) => (
        <UserRow key={user.id} user={user} />
      ))}
    </div>
  );
}

function UserRow({ user }: Readonly<{ user: TenantUserSummary }>) {
  const displayName =
    user.firstName || user.lastName ? `${user.firstName ?? ""} ${user.lastName ?? ""}`.trim() : user.email;

  return (
    <div className="flex items-center gap-2.5">
      <Avatar size="sm">
        <AvatarImage src={user.avatarUrl ?? undefined} alt="" />
        <AvatarFallback>
          {getInitials(user.firstName ?? undefined, user.lastName ?? undefined, user.email)}
        </AvatarFallback>
      </Avatar>
      <div className="flex min-w-0 flex-1 flex-col">
        <span className="truncate text-sm font-medium">{displayName}</span>
        <span className="truncate text-xs text-muted-foreground">{user.email}</span>
      </div>
    </div>
  );
}
