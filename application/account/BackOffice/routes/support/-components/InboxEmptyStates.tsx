import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { Empty, EmptyContent, EmptyDescription, EmptyHeader, EmptyMedia, EmptyTitle } from "@repo/ui/components/Empty";
import { InboxIcon } from "lucide-react";

export function InboxZeroEmpty() {
  return (
    <Empty>
      <EmptyHeader>
        <EmptyMedia variant="icon">
          <InboxIcon />
        </EmptyMedia>
        <EmptyTitle>
          <Trans>Inbox zero</Trans>
        </EmptyTitle>
        <EmptyDescription>
          <Trans>No active tickets across any account.</Trans>
        </EmptyDescription>
      </EmptyHeader>
    </Empty>
  );
}

export function NoMatchesEmpty({ onClearFilters }: Readonly<{ onClearFilters: () => void }>) {
  return (
    <Empty>
      <EmptyHeader>
        <EmptyMedia variant="icon">
          <InboxIcon />
        </EmptyMedia>
        <EmptyTitle>
          <Trans>No matches</Trans>
        </EmptyTitle>
        <EmptyDescription>
          <Trans>Try clearing filters or searching for something else.</Trans>
        </EmptyDescription>
      </EmptyHeader>
      <EmptyContent>
        <Button variant="outline" size="sm" onClick={onClearFilters}>
          <Trans>Clear filters</Trans>
        </Button>
      </EmptyContent>
    </Empty>
  );
}
