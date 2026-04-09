import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { NumberField } from "@repo/ui/components/NumberField";
import { SwitchField } from "@repo/ui/components/SwitchField";
import { Tooltip, TooltipContent, TooltipTrigger } from "@repo/ui/components/Tooltip";
import { formatDate } from "@repo/utils/date/formatDate";
import { InfoIcon } from "lucide-react";
import { useEffect, useRef, useState } from "react";
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
          <div className="flex shrink-0 items-end gap-4">
            <RolloutPercentageInput flagKey={featureFlag.key} currentPercentage={featureFlag.rolloutPercentage} />
            <SwitchField
              label={featureFlag.isActive ? t`Active` : t`Inactive`}
              checked={featureFlag.isActive}
              onCheckedChange={handleToggle}
              disabled={isPending}
              aria-label={t`Toggle ${getFeatureFlagName(featureFlag.key)}`}
            />
          </div>
        ) : (
          <SwitchField
            label={featureFlag.isActive ? t`Active` : t`Inactive`}
            checked={featureFlag.isActive}
            onCheckedChange={handleToggle}
            disabled={isPending}
            aria-label={t`Toggle ${getFeatureFlagName(featureFlag.key)}`}
          />
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
  const [percentage, setPercentage] = useState(currentPercentage ?? 0);
  const lastSavedValue = useRef(currentPercentage ?? 0);
  const saveTimerRef = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);

  const rolloutMutation = api.useMutation("put", "/api/back-office/feature-flags/{featureFlagKey}/rollout-percentage");

  useEffect(() => () => clearTimeout(saveTimerRef.current), []);

  const handleChange = (value: number | null) => {
    const newValue = value ?? 0;
    setPercentage(newValue);
    clearTimeout(saveTimerRef.current);
    saveTimerRef.current = setTimeout(() => {
      if (newValue === lastSavedValue.current) return;
      rolloutMutation.mutate(
        {
          params: { path: { featureFlagKey: flagKey } },
          body: { rolloutPercentage: newValue }
        },
        {
          onSuccess: () => {
            lastSavedValue.current = newValue;
            toast.success(t`Rollout percentage updated`);
          },
          onError: () => {
            setPercentage(lastSavedValue.current);
          }
        }
      );
    }, 800);
  };

  return (
    <NumberField
      label={t`Rollout %`}
      name="rolloutPercentage"
      value={percentage}
      onChange={handleChange}
      minValue={0}
      maxValue={100}
      className="w-[6rem]"
    />
  );
}
