import { t } from "@lingui/core/macro";
import { Tooltip, TooltipContent, TooltipTrigger } from "@repo/ui/components/Tooltip";
import { GlobeIcon, UserIcon, UsersIcon } from "lucide-react";

import type { FeatureFlagScope } from "./types";

const scopeConfig: Record<FeatureFlagScope, { icon: typeof UsersIcon; label: () => string }> = {
  Tenant: { icon: UsersIcon, label: () => t`Account` },
  User: { icon: UserIcon, label: () => t`User` },
  System: { icon: GlobeIcon, label: () => t`System` }
};

export function ScopeIcon({ scope, className }: Readonly<{ scope: FeatureFlagScope; className?: string }>) {
  const config = scopeConfig[scope];
  const Icon = config.icon;
  const label = config.label();
  return (
    <Tooltip>
      <TooltipTrigger>
        <Icon className={className ?? "size-4 text-muted-foreground"} aria-label={label} />
      </TooltipTrigger>
      <TooltipContent>{label}</TooltipContent>
    </Tooltip>
  );
}
