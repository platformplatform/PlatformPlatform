import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Separator } from "@repo/ui/components/Separator";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { Switch } from "@repo/ui/components/Switch";
import { getFeatureFlagLabel } from "@repo/ui/featureFlags/labels";
import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import { api } from "@/shared/lib/api/client";

interface TenantFlag {
  flagKey: string;
  enabled: boolean;
}

export function FeaturesSection() {
  const { data, isLoading } = api.useQuery("get", "/api/account/feature-flags/tenant-configurable");

  if (isLoading) {
    return <FeaturesSkeleton />;
  }

  const tenantFlags = data?.flags ?? [];
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
        <Trans>Toggle features available to your account. Changes can take up to 5 minutes to reach all users.</Trans>
      </p>
      <div className="flex flex-col gap-2">
        {tenantFlags.map((f) => (
          <TenantFlagToggle key={f.flagKey} flagKey={f.flagKey} enabled={f.enabled} />
        ))}
      </div>
    </div>
  );
}

function TenantFlagToggle({ flagKey, enabled }: Readonly<TenantFlag>) {
  const queryClient = useQueryClient();
  const label = getFeatureFlagLabel(flagKey);

  const toggleMutation = api.useMutation("put", "/api/account/feature-flags/{flagKey}/tenant-override", {
    onSuccess: async () => {
      toast.success(t`Feature updated`);
      await queryClient.invalidateQueries({ queryKey: ["get", "/api/account/feature-flags/tenant-configurable"] });
    }
  });

  const handleToggle = (checked: boolean) => {
    toggleMutation.mutate({ params: { path: { flagKey } }, body: { enabled: checked } });
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
