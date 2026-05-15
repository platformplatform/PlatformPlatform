import { t } from "@lingui/core/macro";
import { Tooltip, TooltipContent, TooltipTrigger } from "@repo/ui/components/Tooltip";
import { FlaskConicalIcon, GlobeIcon, UserIcon, UsersIcon } from "lucide-react";

import type { FeatureFlagScope } from "./types";

const scopeConfig: Record<FeatureFlagScope, { icon: typeof UsersIcon; label: () => string }> = {
  Tenant: { icon: UsersIcon, label: () => t`Account` },
  User: { icon: UserIcon, label: () => t`User` },
  System: { icon: GlobeIcon, label: () => t`System` }
};

export function ScopeIcon({
  scope,
  isAbTestEligible,
  className
}: Readonly<{ scope: FeatureFlagScope; isAbTestEligible?: boolean; className?: string }>) {
  const Icon = isAbTestEligible ? FlaskConicalIcon : scopeConfig[scope].icon;
  const label = isAbTestEligible ? t`A/B test` : scopeConfig[scope].label();
  return (
    <Tooltip>
      <TooltipTrigger>
        <Icon className={className ?? "size-4 text-muted-foreground"} aria-label={label} />
      </TooltipTrigger>
      <TooltipContent>{label}</TooltipContent>
    </Tooltip>
  );
}
