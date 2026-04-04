import { t } from "@lingui/core/macro";
import { Empty, EmptyDescription, EmptyHeader, EmptyMedia, EmptyTitle } from "@repo/ui/components/Empty";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { SearchIcon } from "lucide-react";

export function UserEmptyState({ variant }: Readonly<{ variant: "no-users" | "no-results" | "loading" }>) {
  if (variant === "loading") {
    return (
      <div className="flex flex-col gap-2">
        <Skeleton className="h-10 w-full rounded-md" />
        <Skeleton className="h-14 w-full rounded-md" />
        <Skeleton className="h-14 w-full rounded-md" />
      </div>
    );
  }

  const title = variant === "no-users" ? t`Search for users` : t`No users found`;
  const description =
    variant === "no-users"
      ? t`Type an email address above to find users and manage their overrides`
      : t`Try adjusting your search`;
  return (
    <Empty>
      <EmptyHeader>
        <EmptyMedia variant="icon">
          <SearchIcon />
        </EmptyMedia>
        <EmptyTitle>{title}</EmptyTitle>
        <EmptyDescription>{description}</EmptyDescription>
      </EmptyHeader>
    </Empty>
  );
}
