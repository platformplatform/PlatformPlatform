import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { featureFlagCollection } from "@repo/infrastructure/sync/collections";
import { useFeatureFlags } from "@repo/infrastructure/sync/hooks";
import { useElectricMutation } from "@repo/infrastructure/sync/useElectricMutation";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { Switch } from "@repo/ui/components/Switch";
import { useMemo } from "react";
import { toast } from "sonner";

import { apiClient } from "@/shared/lib/api/client";
import { getFeatureFlagLabel } from "@/shared/lib/api/featureFlagLabels";

interface UserFlag {
  flagKey: string;
  enabled: boolean;
}

function isEnabled(flag: { enabledAt: string | null; disabledAt: string | null }): boolean {
  return flag.enabledAt !== null && (flag.disabledAt === null || flag.enabledAt > flag.disabledAt);
}

export function BetaFeaturesSection() {
  const userId = import.meta.user_info_env.id;
  const { data: featureFlags, isLoading } = useFeatureFlags();

  const userFlags = useMemo(() => {
    const baseFlags = featureFlags.filter(
      (flag) => flag.tenantId === null && flag.userId === null && flag.configurableByUser === "true"
    );

    return baseFlags.map((baseFlag): UserFlag => {
      const override = featureFlags.find((flag) => flag.flagKey === baseFlag.flagKey && flag.userId === userId);
      return {
        flagKey: baseFlag.flagKey,
        enabled: override ? isEnabled(override) : false
      };
    });
  }, [featureFlags, userId]);

  if (isLoading) {
    return <BetaFeaturesSkeleton />;
  }

  if (userFlags.length === 0) {
    return null;
  }

  return (
    <section>
      <h3 className="mb-1">
        <Trans>Beta features</Trans>
      </h3>
      <p className="mb-4 text-sm text-muted-foreground">
        <Trans>Opt in to try new features before they are available to everyone.</Trans>
      </p>
      <div className="flex flex-col gap-2">
        {userFlags.map((flag) => (
          <UserFlagToggle key={flag.flagKey} flagKey={flag.flagKey} enabled={flag.enabled} />
        ))}
      </div>
    </section>
  );
}

function UserFlagToggle({ flagKey, enabled }: Readonly<UserFlag>) {
  const label = getFeatureFlagLabel(flagKey);

  const toggleMutation = useElectricMutation({
    mutationFn: async (variables: { flagKey: string; enabled: boolean }) => {
      const { error } = await apiClient.PUT("/api/account/feature-flags/{flagKey}/user-override", {
        params: { path: { flagKey: variables.flagKey } },
        body: { enabled: variables.enabled }
      });
      if (error) {
        throw error;
      }
    },
    utils: featureFlagCollection.utils,
    onSuccess: () => {
      toast.success(t`Preference updated`);
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

function BetaFeaturesSkeleton() {
  return (
    <section>
      <Skeleton className="mb-1 h-6 w-32" />
      <Skeleton className="mb-4 h-4 w-80" />
      <div className="flex flex-col gap-2">
        <Skeleton className="h-[4.5rem] w-full rounded-lg" />
        <Skeleton className="h-[4.5rem] w-full rounded-lg" />
      </div>
    </section>
  );
}
