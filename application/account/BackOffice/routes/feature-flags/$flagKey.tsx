import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { Badge } from "@repo/ui/components/Badge";
import { Button } from "@repo/ui/components/Button";
import { SidebarInset, SidebarProvider } from "@repo/ui/components/Sidebar";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { Switch } from "@repo/ui/components/Switch";
import { TextField } from "@repo/ui/components/TextField";
import { createFileRoute, Link } from "@tanstack/react-router";
import { ArrowLeftIcon } from "lucide-react";
import { useState } from "react";
import { toast } from "sonner";

import { BackOfficeSideMenu } from "@/shared/components/BackOfficeSideMenu";
import { api } from "@/shared/lib/api/client";

import type { FeatureFlagInfo, GetFeatureFlagsResponse, GetFlagTenantsResponse } from "./-components/types";

import { TenantOverridesSection } from "./-components/TenantOverridesSection";

export const Route = createFileRoute("/feature-flags/$flagKey")({
  staticData: { trackingTitle: "Feature flag detail" },
  component: FlagDetailPage
});

export default function FlagDetailPage() {
  const { flagKey } = Route.useParams();

  const { data: flagsData, isLoading: isLoadingFlags } = api.useQuery("get", "/api/back-office/feature-flags") as {
    data: GetFeatureFlagsResponse | undefined;
    isLoading: boolean;
  };

  const { data: tenantsData, isLoading: isLoadingTenants } = api.useQuery(
    "get",
    "/api/back-office/feature-flags/{flagKey}/tenants",
    { params: { path: { flagKey } } }
  ) as {
    data: GetFlagTenantsResponse | undefined;
    isLoading: boolean;
  };

  const flag = flagsData?.flags?.find((f) => f.key === flagKey);
  const isLoading = isLoadingFlags || isLoadingTenants;

  return (
    <SidebarProvider>
      <BackOfficeSideMenu />
      <SidebarInset>
        <AppLayout
          browserTitle={flag?.description ?? t`Feature flag detail`}
          title={
            <div className="flex items-center gap-3">
              <Link to="/feature-flags" className="text-muted-foreground hover:text-foreground">
                <ArrowLeftIcon className="size-5" aria-label={t`Back to feature flags`} />
              </Link>
              <span>{flag?.description ?? flagKey}</span>
            </div>
          }
          subtitle={flag?.key}
        >
          {isLoading ? (
            <FlagDetailSkeleton />
          ) : flag ? (
            <div className="flex flex-col gap-8">
              <FlagInfoSection flag={flag} />
              {flag.scope === "Tenant" && (
                <TenantOverridesSection
                  flagKey={flag.key}
                  flagDescription={flag.description}
                  tenants={tenantsData?.tenants ?? []}
                />
              )}
            </div>
          ) : null}
        </AppLayout>
      </SidebarInset>
    </SidebarProvider>
  );
}

function FlagInfoSection({ flag }: Readonly<{ flag: FeatureFlagInfo }>) {
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
    <div className="flex flex-col gap-4 rounded-lg border p-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <span className="text-sm text-muted-foreground">
            <Trans>Type</Trans>
          </span>
          <Badge variant="secondary">{flag.scope}</Badge>
        </div>
        <div className="flex items-center gap-3">
          <span className="text-sm text-muted-foreground">{flag.isActive ? t`Active` : t`Inactive`}</span>
          <Switch
            checked={flag.isActive}
            onCheckedChange={handleToggle}
            disabled={isPending}
            aria-label={t`Toggle ${flag.description}`}
          />
        </div>
      </div>
      <FlagTimestamps flag={flag} />
      {flag.isAbTestEligible && (
        <RolloutPercentageInput flagKey={flag.key} currentPercentage={flag.rolloutPercentage} />
      )}
    </div>
  );
}

function FlagTimestamps({ flag }: Readonly<{ flag: FeatureFlagInfo }>) {
  const timestamps = [];
  if (flag.enabledAt) {
    timestamps.push(`${t`Enabled`}: ${formatTimestamp(flag.enabledAt)}`);
  }
  if (flag.disabledAt) {
    timestamps.push(`${t`Disabled`}: ${formatTimestamp(flag.disabledAt)}`);
  }

  if (timestamps.length === 0) return null;

  return <span className="text-sm text-muted-foreground">{timestamps.join(" | ")}</span>;
}

function formatTimestamp(isoDate: string): string {
  const date = new Date(isoDate);
  return new Intl.DateTimeFormat(navigator.language, { year: "numeric", month: "short", day: "numeric" }).format(date);
}

function RolloutPercentageInput({
  flagKey,
  currentPercentage
}: Readonly<{
  flagKey: string;
  currentPercentage: number | null;
}>) {
  const [percentage, setPercentage] = useState(String(currentPercentage ?? 0));

  const rolloutMutation = api.useMutation("put", "/api/back-office/feature-flags/{flagKey}/rollout-percentage");

  const handleSave = () => {
    const value = Number.parseInt(percentage, 10);
    if (Number.isNaN(value) || value < 0 || value > 100) return;

    rolloutMutation.mutate(
      {
        params: { path: { flagKey } },
        body: { rolloutPercentage: value }
      },
      {
        onSuccess: () => {
          toast.success(t`Rollout percentage updated`);
        }
      }
    );
  };

  return (
    <div className="flex items-end gap-3">
      <TextField
        label={t`Rollout percentage`}
        name="rolloutPercentage"
        type="number"
        value={percentage}
        onChange={(value) => setPercentage(value)}
        className="w-32"
      />
      <Button onClick={handleSave} disabled={rolloutMutation.isPending}>
        {rolloutMutation.isPending ? <Trans>Saving...</Trans> : <Trans>Save</Trans>}
      </Button>
    </div>
  );
}

function FlagDetailSkeleton() {
  return (
    <div className="flex flex-col gap-8">
      <Skeleton className="h-32 w-full rounded-lg" />
      <div className="flex flex-col gap-4">
        <Skeleton className="h-6 w-40" />
        <Skeleton className="h-10 w-full rounded-md" />
        <Skeleton className="h-14 w-full rounded-md" />
        <Skeleton className="h-14 w-full rounded-md" />
      </div>
    </div>
  );
}
