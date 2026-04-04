import { t } from "@lingui/core/macro";
import { Empty, EmptyDescription, EmptyHeader, EmptyMedia, EmptyTitle } from "@repo/ui/components/Empty";
import { SearchIcon } from "lucide-react";

export function UserEmptyState({ variant }: Readonly<{ variant: "no-users" | "no-results" }>) {
  const title = variant === "no-users" ? t`No user overrides` : t`No users found`;
  const description =
    variant === "no-users"
      ? t`Use the search above to find users and manage their overrides`
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
