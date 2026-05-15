import { t } from "@lingui/core/macro";
import { Link } from "@tanstack/react-router";
import { ArrowLeftIcon } from "lucide-react";

import type { FeatureFlagInfo } from "./types";

import { ScopeIcon } from "./ScopeIcon";

interface FeatureFlagDetailTitleProps {
  featureFlag: FeatureFlagInfo | undefined;
  featureFlagName: string;
}

export function FeatureFlagDetailTitle({ featureFlag, featureFlagName }: Readonly<FeatureFlagDetailTitleProps>) {
  return (
    <div className="flex flex-wrap items-center gap-3">
      <Link to="/feature-flags" className="text-muted-foreground hover:text-foreground">
        <ArrowLeftIcon className="size-5" aria-label={t`Back to feature flags`} />
      </Link>
      {featureFlag && (
        <ScopeIcon
          scope={featureFlag.scope}
          isAbTestEligible={featureFlag.isAbTestEligible}
          className="size-6 stroke-[2.5] text-foreground"
        />
      )}
      <span>{featureFlagName}</span>
    </div>
  );
}
