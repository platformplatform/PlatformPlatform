import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Badge } from "@repo/ui/components/Badge";
import { Switch } from "@repo/ui/components/Switch";
import { TextField } from "@repo/ui/components/TextField";
import { Tooltip, TooltipContent, TooltipTrigger } from "@repo/ui/components/Tooltip";
import { getFeatureFlagName } from "@repo/ui/featureFlags/labels";
import { InfoIcon } from "lucide-react";
import { useRef, useState } from "react";
import { toast } from "sonner";

import { api } from "@/shared/lib/api/client";

import type { FeatureFlagInfo } from "./types";

import { formatRolloutBucketRange } from "./rolloutBucket";

interface FeatureFlagInfoSectionProps {
  featureFlag: FeatureFlagInfo;
  isKillSwitchEnabled: boolean;
  orphanedAt: string | null;
}

export function FeatureFlagInfoSection({
  featureFlag,
  isKillSwitchEnabled,
  orphanedAt
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
          toast.success(checked ? t`Feature flag activated` : t`Feature flag deactivated`);
        }
      }
    );
  };

  const showToggle = isKillSwitchEnabled && orphanedAt === null;

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <FeatureFlagMetadata featureFlag={featureFlag} />
        {showToggle && featureFlag.isAbTestEligible ? (
          <div className="flex shrink-0 flex-col gap-1">
            <span className="text-sm font-medium text-foreground">
              <Trans>Rollout %</Trans>
            </span>
            <div className="flex items-center gap-4">
              <RolloutPercentageInput flagKey={featureFlag.key} currentPercentage={featureFlag.rolloutPercentage} />
              <Badge variant={featureFlag.isActive ? "default" : "outline"}>
                {featureFlag.isActive ? t`Active` : t`Inactive`}
              </Badge>
              <Switch
                checked={featureFlag.isActive}
                onCheckedChange={handleToggle}
                disabled={isPending}
                aria-label={t`Toggle ${getFeatureFlagName(featureFlag.key)}`}
              />
            </div>
          </div>
        ) : showToggle ? (
          <div className="flex shrink-0 items-center gap-4">
            <Badge variant={featureFlag.isActive ? "default" : "outline"}>
              {featureFlag.isActive ? t`Active` : t`Inactive`}
            </Badge>
            <Switch
              checked={featureFlag.isActive}
              onCheckedChange={handleToggle}
              disabled={isPending}
              aria-label={t`Toggle ${getFeatureFlagName(featureFlag.key)}`}
            />
          </div>
        ) : (
          <Badge variant={featureFlag.isActive ? "default" : "outline"} className="shrink-0">
            {featureFlag.isActive ? t`Active` : t`Inactive`}
          </Badge>
        )}
      </div>
      {!isKillSwitchEnabled && orphanedAt === null && (
        <p className="text-sm text-muted-foreground">
          <Trans>
            This flag is platform-managed. Plan-based flags update with the account's subscription. Stable features are
            not togglable.
          </Trans>
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

  const rolloutBucketLine =
    featureFlag.isAbTestEligible &&
    featureFlag.bucketStart != null &&
    featureFlag.bucketEnd != null &&
    featureFlag.rolloutPercentage != null
      ? formatRolloutBucketRange(featureFlag.bucketStart, featureFlag.bucketEnd, featureFlag.rolloutPercentage)
      : null;

  return (
    <div className="flex flex-col gap-0.5 text-sm text-muted-foreground">
      <span>
        <Trans>Name:</Trans> <span className="font-mono">{featureFlag.key}</span>
      </span>
      {enabledLine && <span>{enabledLine}</span>}
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

function RolloutPercentageInput({
  flagKey,
  currentPercentage
}: Readonly<{
  flagKey: string;
  currentPercentage: number | null;
}>) {
  const [percentage, setPercentage] = useState(String(currentPercentage ?? 0));
  const lastSavedValue = useRef(String(currentPercentage ?? 0));

  const rolloutMutation = api.useMutation("put", "/api/back-office/feature-flags/{flagKey}/rollout-percentage");

  const handleBlur = () => {
    const value = Number.parseInt(percentage, 10);
    if (Number.isNaN(value) || value < 0 || value > 100) {
      setPercentage(lastSavedValue.current);
      return;
    }
    if (percentage === lastSavedValue.current) return;

    rolloutMutation.mutate(
      {
        params: { path: { flagKey } },
        body: { rolloutPercentage: value }
      },
      {
        onSuccess: () => {
          lastSavedValue.current = String(value);
          toast.success(t`Rollout percentage updated`);
        },
        onError: () => {
          setPercentage(lastSavedValue.current);
        }
      }
    );
  };

  return (
    <TextField
      aria-label={t`Rollout %`}
      name="rolloutPercentage"
      type="number"
      value={percentage}
      onChange={(value) => setPercentage(value)}
      onBlur={handleBlur}
      className="w-[6rem] whitespace-nowrap"
    />
  );
}
