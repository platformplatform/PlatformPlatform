import type { FeatureFlagType } from "@repo/infrastructure/featureFlags/featureFlagDefinitions";

import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { Badge } from "@repo/ui/components/Badge";
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage
} from "@repo/ui/components/Breadcrumb";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@repo/ui/components/Table";
import { createFileRoute, Link, useNavigate } from "@tanstack/react-router";
import { useMemo } from "react";

import { api } from "@/shared/lib/api/client";

import type { FeatureFlagInfo, FeatureFlagScope, GetFeatureFlagsResponse } from "./-components/types";

import { getFeatureFlagDescription, getFeatureFlagName } from "./-components/flagLabels";
import { ScopeIcon } from "./-components/ScopeIcon";

export const Route = createFileRoute("/back-office/feature-flags/")({
  staticData: { trackingTitle: "Feature flags" },
  component: FeatureFlagsPage
});

type FeatureFlagGroupKey = {
  system: "System";
  subscriptionPlan: "Plan";
  tenant: "Tenant";
  user: "User";
}[FeatureFlagType];

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
      Tenant: t`Account feature flags`,
      Plan: t`Subscription plan feature flags`,
      User: t`User feature flags`,
      System: t`System feature flags`
    };
    return groupOrder
      .map((groupKey) => ({
        groupKey,
        label: groupLabels[groupKey],
        featureFlags: data.flags.filter((featureFlag) => {
          if (groupKey === "Plan")
            return featureFlag.scope === "Tenant" && featureFlag.requiredSubscriptionPlan != null;
          if (groupKey === "Tenant")
            return featureFlag.scope === "Tenant" && featureFlag.requiredSubscriptionPlan == null;
          return featureFlag.scope === groupKey;
        })
      }))
      .filter((group) => group.featureFlags.length > 0);
  }, [data?.flags]);

  return (
    <AppLayout
      title={t`Feature flags`}
      subtitle={t`Manage feature flags across the platform.`}
      maxWidth="64rem"
      beforeHeader={
        <Breadcrumb>
          <BreadcrumbList>
            <BreadcrumbItem>
              <BreadcrumbLink render={<Link to="/back-office" />}>
                <Trans>Back office</Trans>
              </BreadcrumbLink>
            </BreadcrumbItem>
            <BreadcrumbItem>
              <BreadcrumbPage>
                <Trans>Feature flags</Trans>
              </BreadcrumbPage>
            </BreadcrumbItem>
          </BreadcrumbList>
        </Breadcrumb>
      }
    >
      {isLoading ? <FeatureFlagsSkeleton /> : <FeatureFlagGroupList groups={groups} />}
    </AppLayout>
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
        return (
          <div key={group.groupKey} className="flex flex-col gap-2">
            <h3>{group.label}</h3>
            <Table rowSize="compact" aria-label={group.label}>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-auto">
                    <Trans>Name</Trans>
                  </TableHead>
                  {!isSystemGroup && (
                    <TableHead className="hidden w-[8rem] sm:table-cell">
                      {isPlanGroup ? <Trans>Required plan</Trans> : <Trans>Rollout</Trans>}
                    </TableHead>
                  )}
                  <TableHead className="w-[5rem] text-right">
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
                        ? () =>
                            navigate({
                              to: "/back-office/feature-flags/$flagKey",
                              params: { flagKey: featureFlag.key }
                            })
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
                    {!isSystemGroup && (
                      <TableCell className="hidden sm:table-cell">
                        {isPlanGroup ? (
                          <Badge variant="outline">{featureFlag.requiredSubscriptionPlan}</Badge>
                        ) : featureFlag.rolloutPercentage !== null ? (
                          `${featureFlag.rolloutPercentage}%`
                        ) : (
                          <span className="text-muted-foreground">--</span>
                        )}
                      </TableCell>
                    )}
                    <TableCell className="text-right">
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
