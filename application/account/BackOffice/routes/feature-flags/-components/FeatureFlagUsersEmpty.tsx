import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { Empty, EmptyContent, EmptyDescription, EmptyHeader, EmptyMedia, EmptyTitle } from "@repo/ui/components/Empty";
import { useNavigate } from "@tanstack/react-router";
import { UsersIcon } from "lucide-react";

export function FeatureFlagUsersEmpty({ flagKey, hasFilters }: Readonly<{ flagKey: string; hasFilters: boolean }>) {
  const navigate = useNavigate();
  return (
    <Empty>
      <EmptyHeader>
        <EmptyMedia variant="icon">
          <UsersIcon />
        </EmptyMedia>
        <EmptyTitle>
          {hasFilters ? (
            <Trans>No users match your search</Trans>
          ) : (
            <Trans>No users qualify for this feature yet</Trans>
          )}
        </EmptyTitle>
        <EmptyDescription>
          {hasFilters ? (
            <Trans>Try clearing the search or filters to see more results.</Trans>
          ) : (
            <Trans>Users will appear here as they qualify for this feature.</Trans>
          )}
        </EmptyDescription>
      </EmptyHeader>
      {hasFilters && (
        <EmptyContent>
          <Button
            variant="outline"
            size="sm"
            onClick={() =>
              navigate({
                to: "/feature-flags/$flagKey",
                params: { flagKey },
                search: () => ({})
              })
            }
          >
            <Trans>Clear filters</Trans>
          </Button>
        </EmptyContent>
      )}
    </Empty>
  );
}
