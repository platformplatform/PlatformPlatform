import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { Empty, EmptyContent, EmptyDescription, EmptyHeader, EmptyMedia, EmptyTitle } from "@repo/ui/components/Empty";
import { useNavigate } from "@tanstack/react-router";
import { Building2Icon } from "lucide-react";

// Shared empty state for the tenant-audience tables on /feature-flags/{flagKey}. Both the Plan
// section and the regular Tenant overrides section render this — the wording is identical, only
// the "no entities yet" / "no entities match" branch differs by filter state.
export function FeatureFlagTenantsEmpty({ flagKey, hasFilters }: Readonly<{ flagKey: string; hasFilters: boolean }>) {
  const navigate = useNavigate();
  return (
    <Empty>
      <EmptyHeader>
        <EmptyMedia variant="icon">
          <Building2Icon />
        </EmptyMedia>
        <EmptyTitle>
          {hasFilters ? (
            <Trans>No accounts match your filters</Trans>
          ) : (
            <Trans>No accounts qualify for this feature yet</Trans>
          )}
        </EmptyTitle>
        <EmptyDescription>
          {hasFilters ? (
            <Trans>Try clearing the search or filters to see more results.</Trans>
          ) : (
            <Trans>Accounts will appear here as they qualify for this feature.</Trans>
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
