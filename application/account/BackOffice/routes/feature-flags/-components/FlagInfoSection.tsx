import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Badge } from "@repo/ui/components/Badge";
import { Switch } from "@repo/ui/components/Switch";
import { TextField } from "@repo/ui/components/TextField";
import { Tooltip, TooltipContent, TooltipTrigger } from "@repo/ui/components/Tooltip";
import { InfoIcon } from "lucide-react";
import { useRef, useState } from "react";
import { toast } from "sonner";

import { api } from "@/shared/lib/api/client";

import type { FeatureFlagInfo } from "./types";

import { getFlagName } from "./flagLabels";
import { formatBucketRange } from "./rolloutBucket";

export function FlagInfoSection({ flag }: Readonly<{ flag: FeatureFlagInfo }>) {
  const activateMutation = api.useMutation("put", "/api/back-office/feature-flags/{flagKey}/activate");
  const deactivateMutation = api.useMutation("put", "/api/back-office/feature-flags/{flagKey}/deactivate");
  const isPending = activateMutation.isPending || deactivateMutation.isPending;

  const handleToggle = (checked: boolean) => {
    const mutation = checked ? activateMutation : deactivateMutation;
    mutation.mutate(
      { params: { path: { flagKey: flag.key } } },
      {
        onSuccess: () => {
          toast.success(checked ? t`Feature flag activated` : t`Feature flag deactivated`);
        }
      }
    );
  };

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center justify-between">
        <FlagMetadata flag={flag} />
        <div className="flex items-center gap-4">
          {flag.isAbTestEligible && (
            <RolloutPercentageInput flagKey={flag.key} currentPercentage={flag.rolloutPercentage} />
          )}
          <Badge variant={flag.isActive ? "default" : "outline"}>{flag.isActive ? t`Active` : t`Inactive`}</Badge>
          <Switch
            checked={flag.isActive}
            onCheckedChange={handleToggle}
            disabled={isPending}
            aria-label={t`Toggle ${getFlagName(flag.key)}`}
          />
        </div>
      </div>
      {flag.bucketStart != null && flag.bucketEnd != null && flag.rolloutPercentage != null && (
        <div className="flex items-center gap-1 text-sm text-muted-foreground">
          <span>{formatBucketRange(flag.bucketStart, flag.bucketEnd, flag.rolloutPercentage)}</span>
          <Tooltip>
            <TooltipTrigger>
              <InfoIcon className="size-4" aria-label={t`Bucket information`} />
            </TooltipTrigger>
            <TooltipContent className="max-w-[20rem]">
              <Trans>
                Each account or user is assigned a fixed bucket (1-100) based on their ID. The rollout targets a
                specific range of buckets, ensuring consistent and predictable feature rollout.
              </Trans>
            </TooltipContent>
          </Tooltip>
        </div>
      )}
    </div>
  );
}

function getScopeLabel(scope: string): string {
  switch (scope) {
    case "Tenant":
      return t`Account`;
    case "User":
      return t`User`;
    case "System":
      return t`System`;
    default:
      return scope;
  }
}

function FlagMetadata({ flag }: Readonly<{ flag: FeatureFlagInfo }>) {
  const items: string[] = [];
  items.push(`${t`Type`}: ${getScopeLabel(flag.scope)}`);
  if (flag.enabledAt) items.push(`${t`Enabled`}: ${formatTimestamp(flag.enabledAt)}`);
  if (flag.disabledAt) items.push(`${t`Disabled`}: ${formatTimestamp(flag.disabledAt)}`);

  return <span className="text-sm text-muted-foreground">{items.join("  \u00B7  ")}</span>;
}

function formatTimestamp(isoDate: string): string {
  const date = new Date(isoDate);
  return new Intl.DateTimeFormat(navigator.language, { year: "numeric", month: "long", day: "numeric" }).format(date);
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
      label={t`Rollout percentage`}
      name="rolloutPercentage"
      type="number"
      value={percentage}
      onChange={(value) => setPercentage(value)}
      onBlur={handleBlur}
      className="w-[6rem]"
    />
  );
}
