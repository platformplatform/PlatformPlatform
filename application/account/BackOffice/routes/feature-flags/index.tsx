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

import { getFlagDescription, getFlagName } from "./-components/flagLabels";
import { ScopeIcon } from "./-components/ScopeIcon";

export const Route = createFileRoute("/feature-flags/")({
  staticData: { trackingTitle: "Feature flags" },
  component: FeatureFlagsPage
});

interface FlagGroup {
  scope: FeatureFlagScope;
  label: string;
  flags: FeatureFlagInfo[];
}

export default function FeatureFlagsPage() {
  const { data, isLoading } = api.useQuery("get", "/api/back-office/feature-flags") as {
    data: GetFeatureFlagsResponse | undefined;
    isLoading: boolean;
  };

  const groups = useMemo(() => {
    if (!data?.flags) return [];
    const scopeOrder: FeatureFlagScope[] = ["Tenant", "User", "System"];
    const scopeLabels: Record<FeatureFlagScope, string> = {
      Tenant: t`Account flags`,
      User: t`User flags`,
      System: t`System flags`
    };
    return scopeOrder
      .map((scope) => ({
        scope,
        label: scopeLabels[scope],
        flags: data.flags.filter((flag) => flag.scope === scope)
      }))
      .filter((group) => group.flags.length > 0);
  }, [data?.flags]);

  return (
    <SidebarProvider>
      <BackOfficeSideMenu />
      <SidebarInset>
        <AppLayout title={t`Feature flags`} subtitle={t`Manage feature flags across the platform.`} maxWidth="64rem">
          {isLoading ? <FeatureFlagsSkeleton /> : <FlagGroupList groups={groups} />}
        </AppLayout>
      </SidebarInset>
    </SidebarProvider>
  );
}

function FlagGroupList({ groups }: Readonly<{ groups: FlagGroup[] }>) {
  const navigate = useNavigate();
  const hasDetail = (scope: FeatureFlagScope) => scope === "Tenant" || scope === "User";

  return (
    <div className="flex flex-col gap-8">
      {groups.map((group) => {
        const showRollout = group.scope !== "System";
        return (
          <div key={group.scope} className="flex flex-col gap-2">
            <h3>{group.label}</h3>
            <Table rowSize="compact" aria-label={group.label}>
              <TableHeader>
                <TableRow>
                  <TableHead>
                    <Trans>Name</Trans>
                  </TableHead>
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
                {group.flags.map((flag) => (
                  <TableRow
                    key={flag.key}
                    className={hasDetail(flag.scope) ? "cursor-pointer" : undefined}
                    onClick={
                      hasDetail(flag.scope)
                        ? () => navigate({ to: "/feature-flags/$flagKey", params: { flagKey: flag.key } })
                        : undefined
                    }
                  >
                    <TableCell>
                      <div className="flex min-w-0 flex-col">
                        <span className="flex items-center gap-2 font-medium">
                          <ScopeIcon scope={flag.scope} />
                          {getFlagName(flag.key)}
                        </span>
                        <span className="truncate text-sm text-muted-foreground">
                          {getFlagDescription(flag.key) || flag.description}
                        </span>
                      </div>
                    </TableCell>
                    {showRollout && (
                      <TableCell className="hidden sm:table-cell">
                        {flag.rolloutPercentage !== null ? (
                          `${flag.rolloutPercentage}%`
                        ) : (
                          <span className="text-muted-foreground">--</span>
                        )}
                      </TableCell>
                    )}
                    <TableCell className="hidden text-right sm:table-cell">
                      <Badge variant={flag.isActive ? "default" : "outline"}>
                        {flag.isActive ? t`Active` : t`Inactive`}
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
