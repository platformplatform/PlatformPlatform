import { Trans } from "@lingui/react/macro";
import { LinkCard } from "@repo/ui/components/LinkCard";

export function LinkCardPreview() {
  return (
    <div className="flex flex-col gap-2">
      <h4>
        <Trans>Link card</Trans>
      </h4>
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
        <LinkCard to="/components" className="gap-2 p-4">
          <span className="text-sm font-medium">
            <Trans>Total users</Trans>
          </span>
          <span className="text-xs text-muted-foreground">
            <Trans>Add more in the Users menu</Trans>
          </span>
          <span className="text-2xl font-semibold tabular-nums">2</span>
        </LinkCard>
        <LinkCard to="/components" className="gap-2 p-4">
          <span className="text-sm font-medium">
            <Trans>Active users</Trans>
          </span>
          <span className="text-xs text-muted-foreground">
            <Trans>Active users in the past 30 days</Trans>
          </span>
          <span className="text-2xl font-semibold tabular-nums">2</span>
        </LinkCard>
        <LinkCard to="/components" className="gap-2 p-4">
          <span className="text-sm font-medium">
            <Trans>Invited users</Trans>
          </span>
          <span className="text-xs text-muted-foreground">
            <Trans>Users who haven't confirmed their email</Trans>
          </span>
          <span className="text-2xl font-semibold tabular-nums">0</span>
        </LinkCard>
      </div>
    </div>
  );
}
