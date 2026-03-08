import { Trans } from "@lingui/react/macro";
import { Empty, EmptyDescription, EmptyHeader, EmptyMedia, EmptyTitle } from "@repo/ui/components/Empty";
import { SearchIcon } from "lucide-react";

export function UserTableEmptyState() {
  return (
    <Empty>
      <EmptyHeader>
        <EmptyMedia variant="icon">
          <SearchIcon />
        </EmptyMedia>
        <EmptyTitle>
          <Trans>No users found</Trans>
        </EmptyTitle>
        <EmptyDescription>
          <Trans>Try adjusting your search or filters</Trans>
        </EmptyDescription>
      </EmptyHeader>
    </Empty>
  );
}
