import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { Badge } from "@repo/ui/components/Badge";
import { SidebarInset, SidebarProvider } from "@repo/ui/components/Sidebar";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@repo/ui/components/Table";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useMemo } from "react";

import { BackOfficeSideMenu } from "@/shared/components/BackOfficeSideMenu";
import { api } from "@/shared/lib/api/client";

import type { FeatureFlagInfo, FeatureFlagScope, GetFeatureFlagsResponse } from "./-components/types";

import { getFeatureFlagDescription, getFeatureFlagName } from "./-components/flagLabels";
import { ScopeIcon } from "./-components/ScopeIcon";

export const Route = createFileRoute("/feature-flags/")({
  staticData: { trackingTitle: "Feature flags" },
  component: FeatureFlagsPage
});

type FeatureFlagGroupKey = "Tenant" | "Plan" | "User" | "System";

interface FeatureFlagGroup {
  groupKey: FeatureFlagGroupKey;
  label: string;
  featureFlags: FeatureFlagInfo[];
}

export default function FeatureFlagsPage() {
  const { data, isLoading } = api.useQuery("get", "/api/back-office/feature-flags") as {
    data: GetFeatureFlagsResponse | undefined;
    isLoading: boolean;
  };

  const groups = useMemo(() => {
    if (!data?.flags) return [];
    const groupOrder: FeatureFlagGroupKey[] = ["Tenant", "Plan", "User", "System"];
    const groupLabels: Record<FeatureFlagGroupKey, string> = {
      Tenant: t`Account flags`,
      Plan: t`Plan flags`,
      User: t`User flags`,
      System: t`System flags`
    };
    return groupOrder
      .map((groupKey) => ({
        groupKey,
        label: groupLabels[groupKey],
        featureFlags: data.flags.filter((featureFlag) => {
          if (groupKey === "Plan") return featureFlag.scope === "Tenant" && featureFlag.requiredPlan != null;
          if (groupKey === "Tenant") return featureFlag.scope === "Tenant" && featureFlag.requiredPlan == null;
          return featureFlag.scope === groupKey;
        })
      }))
      .filter((group) => group.featureFlags.length > 0);
  }, [data?.flags]);

  return (
    <SidebarProvider>
      <BackOfficeSideMenu />
      <SidebarInset>
        <AppLayout
          variant="center"
          maxWidth="64rem"
          title={t`Feature flags`}
          subtitle={t`Feature flags are defined in code. This view controls their activation and rollout.`}
        >
          {isLoading ? <FeatureFlagsSkeleton /> : <FeatureFlagGroupList groups={groups} />}
        </AppLayout>
      </SidebarInset>
    </SidebarProvider>
  );
}

function FeatureFlagGroupList({ groups }: Readonly<{ groups: FeatureFlagGroup[] }>) {
  const navigate = useNavigate();
  const hasDetail = (groupKey: FeatureFlagGroupKey, scope: FeatureFlagScope) =>
    groupKey === "Plan" || scope === "Tenant" || scope === "User";

  return (
    <div className="flex flex-col gap-8">
      {groups.map((group) => {
        const isPlanGroup = group.groupKey === "Plan";
        const isSystemGroup = group.groupKey === "System";
        const showRollout = !isSystemGroup && !isPlanGroup;
        return (
          <div key={group.groupKey} className="flex flex-col gap-2">
            <h3>{group.label}</h3>
            <p className="text-sm text-muted-foreground">
              <FeatureFlagGroupSubtitle groupKey={group.groupKey} />
            </p>
            <Table rowSize="compact" aria-label={group.label}>
              <TableHeader>
                <TableRow>
                  <TableHead>
                    <Trans>Name</Trans>
                  </TableHead>
                  {isPlanGroup && (
                    <TableHead className="hidden w-[8rem] sm:table-cell">
                      <Trans>Required plan</Trans>
                    </TableHead>
                  )}
                  {showRollout && (
                    <TableHead className="hidden w-[5rem] sm:table-cell">
                      <Trans>Rollout</Trans>
                    </TableHead>
                  )}
                  <TableHead className="hidden w-[6rem] text-right sm:table-cell">
                    <Trans>Status</Trans>
                  </TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {group.featureFlags.map((featureFlag) => (
                  <TableRow
                    key={featureFlag.key}
                    className={hasDetail(group.groupKey, featureFlag.scope) ? "cursor-pointer" : undefined}
                    onClick={
                      hasDetail(group.groupKey, featureFlag.scope)
                        ? () => navigate({ to: "/feature-flags/$flagKey", params: { flagKey: featureFlag.key } })
                        : undefined
                    }
                  >
                    <TableCell>
                      <div className="flex min-w-0 flex-col">
                        <span className="flex items-center gap-2 font-medium">
                          <ScopeIcon scope={featureFlag.scope} />
                          {getFeatureFlagName(featureFlag.key)}
                        </span>
                        <span className="hidden truncate text-sm text-muted-foreground sm:block">
                          {getFeatureFlagDescription(featureFlag.key) || featureFlag.description}
                        </span>
                      </div>
                    </TableCell>
                    {isPlanGroup && (
                      <TableCell className="hidden sm:table-cell">
                        <Badge variant="outline">{featureFlag.requiredPlan}</Badge>
                      </TableCell>
                    )}
                    {showRollout && (
                      <TableCell className="hidden sm:table-cell">
                        {featureFlag.rolloutPercentage !== null ? (
                          `${featureFlag.rolloutPercentage}%`
                        ) : (
                          <span className="text-muted-foreground">--</span>
                        )}
                      </TableCell>
                    )}
                    <TableCell className="hidden text-right sm:table-cell">
                      <Badge variant={featureFlag.isActive ? "default" : "outline"}>
                        {featureFlag.isActive ? t`Active` : t`Inactive`}
                      </Badge>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        );
      })}
    </div>
  );
}

function FeatureFlagGroupSubtitle({ groupKey }: Readonly<{ groupKey: FeatureFlagGroupKey }>) {
  switch (groupKey) {
    case "Tenant":
      return <Trans>Per-tenant flags. Owners can toggle configurable flags. Admins control A/B rollouts.</Trans>;
    case "Plan":
      return <Trans>Gated by subscription plan and recomputed when the plan changes. Configured only in code.</Trans>;
    case "User":
      return <Trans>Per-user flags. Users can toggle configurable flags. Admins control A/B rollouts.</Trans>;
    case "System":
      return (
        <Trans>Platform-wide capabilities set at deployment via environment variables. Configured only in code.</Trans>
      );
  }
}

function FeatureFlagsSkeleton() {
  return (
    <div className="flex flex-col gap-2">
      <Skeleton className="h-10 w-full rounded-md" />
      <Skeleton className="h-14 w-full rounded-md" />
      <Skeleton className="h-14 w-full rounded-md" />
      <Skeleton className="h-14 w-full rounded-md" />
    </div>
  );
}
