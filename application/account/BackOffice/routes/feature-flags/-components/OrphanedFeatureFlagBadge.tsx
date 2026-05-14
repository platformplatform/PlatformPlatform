import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Badge } from "@repo/ui/components/Badge";
import { Tooltip, TooltipContent, TooltipTrigger } from "@repo/ui/components/Tooltip";

interface OrphanedFeatureFlagBadgeProps {
  orphanedAt: string;
}

export function OrphanedFeatureFlagBadge({ orphanedAt }: Readonly<OrphanedFeatureFlagBadgeProps>) {
  const formatted = new Intl.DateTimeFormat(navigator.language, {
    year: "numeric",
    month: "long",
    day: "numeric"
  }).format(new Date(orphanedAt));

  return (
    <Tooltip>
      <TooltipTrigger>
        <Badge variant="destructive" aria-label={t`Removed from definitions`}>
          <Trans>Removed from definitions</Trans>
        </Badge>
      </TooltipTrigger>
      <TooltipContent className="max-w-[20rem]">
        <Trans>
          This flag no longer exists in code as of {formatted}. Account and user state is preserved but no longer
          evaluated. Use the delete action to remove the flag and all overrides.
        </Trans>
      </TooltipContent>
    </Tooltip>
  );
}
