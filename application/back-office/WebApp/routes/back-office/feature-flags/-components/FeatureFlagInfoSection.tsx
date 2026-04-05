import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Badge } from "@repo/ui/components/Badge";
import { Switch } from "@repo/ui/components/Switch";
import { TextField } from "@repo/ui/components/TextField";
import { Tooltip, TooltipContent, TooltipTrigger } from "@repo/ui/components/Tooltip";
import { formatDate } from "@repo/utils/date/formatDate";
import { InfoIcon } from "lucide-react";
import { useRef, useState } from "react";
import { toast } from "sonner";

import { api } from "@/shared/lib/api/client";

import type { FeatureFlagInfo } from "./types";

import { getFeatureFlagName } from "./flagLabels";
import { formatRolloutBucketRange } from "./rolloutBucket";

export function FeatureFlagInfoSection({ featureFlag }: Readonly<{ featureFlag: FeatureFlagInfo }>) {
  const activateMutation = api.useMutation("put", "/api/back-office/feature-flags/{featureFlagKey}/activate");
  const deactivateMutation = api.useMutation("put", "/api/back-office/feature-flags/{featureFlagKey}/deactivate");
  const isPending = activateMutation.isPending || deactivateMutation.isPending;

  const handleToggle = (checked: boolean) => {
    const mutation = checked ? activateMutation : deactivateMutation;
    mutation.mutate(
      { params: { path: { featureFlagKey: featureFlag.key } } },
      {
        onSuccess: () => {
          toast.success(checked ? t`Feature flag activated` : t`Feature flag deactivated`);
        }
      }
    );
  };

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <FeatureFlagMetadata featureFlag={featureFlag} />
        {featureFlag.isAbTestEligible ? (
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
        ) : (
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
        )}
      </div>
    </div>
  );
}

function FeatureFlagMetadata({ featureFlag }: Readonly<{ featureFlag: FeatureFlagInfo }>) {
  const enabledLine =
    featureFlag.enabledAt && featureFlag.disabledAt
      ? t`Enabled period: ${formatDate(featureFlag.enabledAt, true)} - ${formatDate(featureFlag.disabledAt, true)}`
      : featureFlag.enabledAt
        ? t`Enabled: ${formatDate(featureFlag.enabledAt, true)}`
        : null;

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

function RolloutPercentageInput({
  flagKey,
  currentPercentage
}: Readonly<{
  flagKey: string;
  currentPercentage: number | null;
}>) {
  const [percentage, setPercentage] = useState(String(currentPercentage ?? 0));
  const lastSavedValue = useRef(String(currentPercentage ?? 0));

  const rolloutMutation = api.useMutation("put", "/api/back-office/feature-flags/{featureFlagKey}/rollout-percentage");

  const handleBlur = () => {
    const value = Number.parseInt(percentage, 10);
    if (Number.isNaN(value) || value < 0 || value > 100) {
      setPercentage(lastSavedValue.current);
      return;
    }
    if (percentage === lastSavedValue.current) return;

    rolloutMutation.mutate(
      {
        params: { path: { featureFlagKey: flagKey } },
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
