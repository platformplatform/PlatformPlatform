import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { trackInteraction } from "@repo/infrastructure/applicationInsights/ApplicationInsightsProvider";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { Switch } from "@repo/ui/components/Switch";
import { getFeatureFlagLabel } from "@repo/ui/featureFlags/labels";
import { toast } from "sonner";

import { api, type Schemas } from "@/shared/lib/api/client";

type UserFlag = Schemas["UserConfigurableFeatureFlag"];

export function PreferencesFeatureFlagsSection() {
  const { data, isLoading } = api.useQuery("get", "/api/account/feature-flags/user-configurable");

  if (isLoading) {
    return <PreferencesFeatureFlagsSkeleton />;
  }

  const userFlags = data?.flags ?? [];
  if (userFlags.length === 0) {
    return null;
  }

  return (
    <section>
      <h3 className="mb-1">
        <Trans>Feature preferences</Trans>
      </h3>
      <p className="mb-4 text-sm text-muted-foreground">
        <Trans>Customize which optional features are enabled for your account.</Trans>
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
  const label = getFeatureFlagLabel(flagKey);

  const toggleMutation = api.useMutation("put", "/api/account/feature-flags/{flagKey}/user-override", {
    onSuccess: () => {
      toast.success(t`Preference updated successfully`, {
        description: `${label.name}. ${t`It takes up to 5 minutes for changes to reach all users.`}`
      });
    }
  });

  const handleToggle = (checked: boolean) => {
    // Use the kebab-case `flagKey` (not the localized `label.name`) so the App Insights event
    // action stays a stable identifier across locales — dashboards group by it without splitting
    // per language.
    trackInteraction("User preferences", "interaction", `Change ${flagKey} to "${checked ? "enabled" : "disabled"}"`);
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

function PreferencesFeatureFlagsSkeleton() {
  return (
    <section>
      <Skeleton className="mb-1 h-6 w-32" />
      <Skeleton className="mb-4 h-4 w-80" />
      <div className="flex flex-col gap-2">
        <Skeleton className="h-[4.5rem] w-full rounded-lg" />
      </div>
    </section>
  );
}
