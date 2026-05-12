import { Avatar, AvatarFallback, AvatarImage } from "@repo/ui/components/Avatar";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { getInitials } from "@repo/utils/string/getInitials";
import { Link } from "@tanstack/react-router";
import { MailIcon } from "lucide-react";

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
        <UserRowSkeleton />
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

function UserRowSkeleton() {
  return (
    <div className="flex items-center gap-3">
      <Skeleton className="size-10 shrink-0 rounded-full" />
      <div className="flex min-w-0 flex-1 flex-col">
        <span className="text-sm leading-5">
          <Skeleton className="inline-block h-[0.875rem] w-32 align-middle" />
        </span>
        <span className="text-xs leading-4">
          <Skeleton className="inline-block h-[0.75rem] w-48 align-middle" />
        </span>
      </div>
    </div>
  );
}

function UserRow({ user }: Readonly<{ user: TenantUserSummary }>) {
  const displayName =
    user.firstName || user.lastName ? `${user.firstName ?? ""} ${user.lastName ?? ""}`.trim() : user.email;

  return (
    <Link
      to="/users/$userId"
      params={{ userId: user.id }}
      className="-mx-2 flex items-center gap-3 rounded-md px-2 py-1 hover:bg-accent active:bg-accent"
    >
      <Avatar size="lg">
        <AvatarImage src={user.avatarUrl ?? undefined} alt="" />
        <AvatarFallback>
          {getInitials(user.firstName ?? undefined, user.lastName ?? undefined, user.email)}
        </AvatarFallback>
      </Avatar>
      <div className="flex min-w-0 flex-1 flex-col leading-tight">
        <span className="truncate text-sm font-medium">{displayName}</span>
        {user.title && <span className="truncate text-xs text-muted-foreground">{user.title}</span>}
        <span className="flex min-w-0 items-center gap-1.5 text-xs text-muted-foreground">
          <MailIcon className="size-3 shrink-0" aria-hidden={true} />
          <span className="truncate">{user.email}</span>
        </span>
      </div>
    </Link>
  );
}
