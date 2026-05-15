import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Badge } from "@repo/ui/components/Badge";
import { NumberField } from "@repo/ui/components/NumberField";
import { SwitchField } from "@repo/ui/components/SwitchField";
import { Tooltip, TooltipContent, TooltipTrigger } from "@repo/ui/components/Tooltip";
import { InfoIcon } from "lucide-react";
import { useState } from "react";
import { toast } from "sonner";

import { api } from "@/shared/lib/api/client";

import type { FeatureFlagInfo } from "./types";

import { FeatureFlagAudienceStats } from "./FeatureFlagAudienceStats";
import { formatRolloutBucketRange } from "./rolloutBucket";

interface FeatureFlagInfoSectionProps {
  featureFlag: FeatureFlagInfo;
  orphanedAt: string | null;
  // True when the signed-in back-office user belongs to the admin group claim. Activate/Deactivate
  // and Delete are gated by AdminPolicyName server-side, so the Switch and Delete button are
  // disabled (not hidden) for non-admin back-office users to avoid silent 403s. Rollout %, overrides,
  // and read operations work for any authenticated back-office identity and stay interactive.
  canActivate: boolean;
}

export function FeatureFlagInfoSection({
  featureFlag,
  orphanedAt,
  canActivate
}: Readonly<FeatureFlagInfoSectionProps>) {
  const activateMutation = api.useMutation("put", "/api/back-office/feature-flags/{flagKey}/activate");
  const deactivateMutation = api.useMutation("put", "/api/back-office/feature-flags/{flagKey}/deactivate");
  const isPending = activateMutation.isPending || deactivateMutation.isPending;

  const handleToggle = (checked: boolean) => {
    const mutation = checked ? activateMutation : deactivateMutation;
    mutation.mutate(
      { params: { path: { flagKey: featureFlag.key } } },
      {
        onSuccess: () => {
          toast.success(checked ? t`Feature flag activated` : t`Feature flag deactivated`, {
            description: t`It takes up to 5 minutes for changes to reach all users.`
          });
        }
      }
    );
  };

  const rolloutMutation = api.useMutation("put", "/api/back-office/feature-flags/{flagKey}/rollout-percentage");
  const [rolloutPercentage, setRolloutPercentage] = useState<number>(featureFlag.rolloutPercentage ?? 0);
  const commitRolloutPercentage = (value: number) => {
    setRolloutPercentage(value);
    if (value === (featureFlag.rolloutPercentage ?? 0)) return;
    rolloutMutation.mutate(
      { params: { path: { flagKey: featureFlag.key } }, body: { rolloutPercentage: value } },
      {
        onSuccess: () =>
          toast.success(t`Rollout percentage updated`, {
            description: t`It takes up to 5 minutes for changes to reach all users.`
          })
      }
    );
  };

  // The Activate/Deactivate endpoints reject any flag whose definition doesn't carry
  // isKillSwitchEnabled: true (Activate/DeactivateFeatureFlagValidator). Hide the toggle for those
  // flags so the UI never exposes a button guaranteed to 400 — non-kill-switch flags get the
  // read-only Badge branch below instead.
  const showToggle = orphanedAt === null && !featureFlag.isStableModule && featureFlag.isKillSwitchEnabled;
  const isFlagAudienceVisible = orphanedAt === null && featureFlag.scope !== "System";

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-start justify-between gap-4">
        <FeatureFlagMetadata featureFlag={featureFlag} />
        <div className="flex shrink-0 items-end gap-6">
          {showToggle && featureFlag.isAbTestEligible && (
            <NumberField
              label={t`Rollout %`}
              tooltip={t`Percentage of accounts or users included in the A/B rollout. Buckets are assigned consistently per account or user.`}
              name="rolloutPercentage"
              value={rolloutPercentage}
              minValue={0}
              maxValue={100}
              onChange={(value) => commitRolloutPercentage(value ?? 0)}
              className="w-[7rem]"
            />
          )}
          {showToggle ? (
            <SwitchField
              label={featureFlag.isActive ? t`Active` : t`Inactive`}
              tooltip={t`Toggle whether this feature flag is currently active. When inactive, accounts and users receive the disabled state regardless of overrides.`}
              checked={featureFlag.isActive}
              onCheckedChange={handleToggle}
              disabled={isPending || !canActivate}
              aria-label={t`Toggle activation`}
            />
          ) : orphanedAt === null ? (
            <Badge variant={featureFlag.isActive ? "default" : "outline"} className="shrink-0">
              {featureFlag.isStableModule ? t`Always on` : featureFlag.isActive ? t`Active` : t`Inactive`}
            </Badge>
          ) : null}
        </div>
      </div>
      {isFlagAudienceVisible && (
        <FeatureFlagAudienceStats
          flagKey={featureFlag.key}
          scope={featureFlag.scope}
          showOverride={featureFlag.isAbTestEligible}
        />
      )}
      {featureFlag.isStableModule && orphanedAt === null && (
        <p className="text-sm text-muted-foreground">
          <Trans>This is a stable module. It is always on and cannot be deactivated.</Trans>
        </p>
      )}
    </div>
  );
}

function FeatureFlagMetadata({ featureFlag }: Readonly<{ featureFlag: FeatureFlagInfo }>) {
  const enabledLine =
    featureFlag.enabledAt && featureFlag.disabledAt
      ? t`Enabled period: ${formatTimestamp(featureFlag.enabledAt)} - ${formatTimestamp(featureFlag.disabledAt)}`
      : featureFlag.enabledAt
        ? t`Enabled: ${formatTimestamp(featureFlag.enabledAt)}`
        : null;

  const deletedLine = featureFlag.deletedAt ? t`Deleted: ${formatTimestamp(featureFlag.deletedAt)}` : null;

  const rolloutBucketLine =
    featureFlag.isAbTestEligible &&
    featureFlag.rolloutBucketStart != null &&
    featureFlag.rolloutBucketEnd != null &&
    featureFlag.rolloutPercentage != null
      ? formatRolloutBucketRange(
          featureFlag.rolloutBucketStart,
          featureFlag.rolloutBucketEnd,
          featureFlag.rolloutPercentage
        )
      : null;

  return (
    <div className="flex flex-col gap-0.5 text-sm text-muted-foreground">
      <span>
        <Trans>Name:</Trans> <span className="font-mono">{featureFlag.key}</span>
      </span>
      {enabledLine && <span>{enabledLine}</span>}
      {deletedLine && <span>{deletedLine}</span>}
      {rolloutBucketLine && (
        <span className="flex items-center gap-1">
          {rolloutBucketLine}
          <Tooltip>
            <TooltipTrigger>
              <InfoIcon className="size-3.5" aria-label={t`Rollout bucket information`} />
            </TooltipTrigger>
            <TooltipContent className="max-w-[20rem]">
              <Trans>
                Each account or user is assigned a fixed bucket (0-99) based on their sequence number. The rollout
                targets a specific range of buckets, ensuring consistent and predictable feature rollout.
              </Trans>
            </TooltipContent>
          </Tooltip>
        </span>
      )}
    </div>
  );
}

function formatTimestamp(isoDate: string): string {
  const date = new Date(isoDate);
  return new Intl.DateTimeFormat(navigator.language, {
    year: "numeric",
    month: "long",
    day: "numeric",
    hour: "numeric",
    minute: "2-digit"
  }).format(date);
}
