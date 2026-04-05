import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { Switch } from "@repo/ui/components/Switch";
import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

import { api } from "@/shared/lib/api/client";
import { getFeatureFlagLabel } from "@/shared/lib/api/featureFlagLabels";

interface UserFlag {
  flagKey: string;
  enabled: boolean;
}

export function BetaFeaturesSection() {
  const { data, isLoading } = api.useQuery("get", "/api/account/feature-flags/user-configurable");

  if (isLoading) {
    return <BetaFeaturesSkeleton />;
  }

  const userFlags = data?.flags ?? [];
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
        {userFlags.map((f) => (
          <UserFlagToggle key={f.flagKey} flagKey={f.flagKey} enabled={f.enabled} />
        ))}
      </div>
    </section>
  );
}

function UserFlagToggle({ flagKey, enabled }: Readonly<UserFlag>) {
  const queryClient = useQueryClient();
  const label = getFeatureFlagLabel(flagKey);

  const toggleMutation = api.useMutation("put", "/api/account/feature-flags/{flagKey}/user-override", {
    onSuccess: async () => {
      toast.success(t`Preference updated`);
      await queryClient.invalidateQueries({ queryKey: ["get", "/api/account/feature-flags/user-configurable"] });
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
