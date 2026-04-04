import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { featureFlagCollection } from "@repo/infrastructure/sync/collections";
import { useFeatureFlags } from "@repo/infrastructure/sync/hooks";
import { useElectricMutation } from "@repo/infrastructure/sync/useElectricMutation";
import { Separator } from "@repo/ui/components/Separator";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { Switch } from "@repo/ui/components/Switch";
import { useMemo } from "react";
import { toast } from "sonner";

import { apiClient } from "@/shared/lib/api/client";
import { getFeatureFlagLabel } from "@/shared/lib/api/featureFlagLabels";

interface TenantFlag {
  flagKey: string;
  enabled: boolean;
}

function isEnabled(flag: { enabledAt: string | null; disabledAt: string | null }): boolean {
  return flag.enabledAt !== null && (flag.disabledAt === null || flag.enabledAt > flag.disabledAt);
}

export function FeaturesSection() {
  const tenantId = import.meta.user_info_env.tenantId;
  const { data: featureFlags, isLoading } = useFeatureFlags();

  const tenantFlags = useMemo(() => {
    const baseFlags = featureFlags.filter(
      (flag) => flag.tenantId === null && flag.userId === null && flag.configurableByTenant && isEnabled(flag)
    );

    return baseFlags.map((baseFlag): TenantFlag => {
      const override = featureFlags.find(
        (flag) => flag.flagKey === baseFlag.flagKey && flag.tenantId === tenantId && flag.userId === null
      );
      return {
        flagKey: baseFlag.flagKey,
        enabled: override ? isEnabled(override) : false
      };
    });
  }, [featureFlags, tenantId]);

  if (isLoading) {
    return <FeaturesSkeleton />;
  }

  if (tenantFlags.length === 0) {
    return null;
  }

  return (
    <div className="mt-12 flex flex-col gap-4">
      <h3>
        <Trans>Features</Trans>
      </h3>
      <Separator />
      <p className="text-sm text-muted-foreground">
        <Trans>Toggle features available to your account.</Trans>
      </p>
      <div className="flex flex-col gap-2">
        {tenantFlags.map((flag) => (
          <TenantFlagToggle key={flag.flagKey} flagKey={flag.flagKey} enabled={flag.enabled} />
        ))}
      </div>
    </div>
  );
}

function TenantFlagToggle({ flagKey, enabled }: Readonly<TenantFlag>) {
  const label = getFeatureFlagLabel(flagKey);

  const toggleMutation = useElectricMutation({
    mutationFn: async (variables: { flagKey: string; enabled: boolean }) => {
      const { error } = await apiClient.PUT("/api/account/feature-flags/{flagKey}/tenant-override", {
        params: { path: { flagKey: variables.flagKey } },
        body: { enabled: variables.enabled }
      });
      if (error) {
        throw error;
      }
    },
    utils: featureFlagCollection.utils,
    onSuccess: () => {
      toast.success(t`Feature updated`);
    }
  });

  const handleToggle = (checked: boolean) => {
    toggleMutation.mutate({ flagKey, enabled: checked });
  };

  return (
    <div className="flex items-center justify-between rounded-lg border p-4">
      <div className="flex flex-col gap-1">
        <span className="text-sm font-medium">{label.name}</span>
        <span className="text-sm text-muted-foreground">{label.description}</span>
      </div>
      <Switch
        checked={enabled}
        onCheckedChange={handleToggle}
        disabled={toggleMutation.isPending}
        aria-label={label.name}
      />
    </div>
  );
}

function FeaturesSkeleton() {
  return (
    <div className="mt-12 flex flex-col gap-4">
      <Skeleton className="h-6 w-24" />
      <Separator />
      <Skeleton className="h-4 w-64" />
      <div className="flex flex-col gap-2">
        <Skeleton className="h-[4.5rem] w-full rounded-lg" />
        <Skeleton className="h-[4.5rem] w-full rounded-lg" />
      </div>
    </div>
  );
}
