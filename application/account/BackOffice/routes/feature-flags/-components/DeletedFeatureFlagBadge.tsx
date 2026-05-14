import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Badge } from "@repo/ui/components/Badge";
import { Tooltip, TooltipContent, TooltipTrigger } from "@repo/ui/components/Tooltip";

interface DeletedFeatureFlagBadgeProps {
  deletedAt: string;
}

export function DeletedFeatureFlagBadge({ deletedAt }: Readonly<DeletedFeatureFlagBadgeProps>) {
  const formatted = new Intl.DateTimeFormat(navigator.language, {
    year: "numeric",
    month: "long",
    day: "numeric"
  }).format(new Date(deletedAt));

  return (
    <Tooltip>
      <TooltipTrigger>
        <Badge variant="outline" aria-label={t`Deleted`}>
          <Trans>Deleted</Trans>
        </Badge>
      </TooltipTrigger>
      <TooltipContent className="max-w-[20rem]">
        <Trans>
          This flag was deleted on {formatted}. The row is retained for historical telemetry; account and user overrides
          have been removed. If the flag is re-added in code, the same row will be restored.
        </Trans>
      </TooltipContent>
    </Tooltip>
  );
}
