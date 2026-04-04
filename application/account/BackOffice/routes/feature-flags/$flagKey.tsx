import { t } from "@lingui/core/macro";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { SidebarInset, SidebarProvider } from "@repo/ui/components/Sidebar";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { useQuery } from "@tanstack/react-query";
import { createFileRoute, Link } from "@tanstack/react-router";
import { ArrowLeftIcon } from "lucide-react";

import { BackOfficeSideMenu } from "@/shared/components/BackOfficeSideMenu";
import { api, apiClient } from "@/shared/lib/api/client";

import type { GetFeatureFlagsResponse, GetFlagTenantsResponse, GetFlagUsersResponse } from "./-components/types";

import { FlagInfoSection } from "./-components/FlagInfoSection";
import { getFlagDescription, getFlagName } from "./-components/flagLabels";
import { ScopeIcon } from "./-components/ScopeIcon";
import { TenantOverridesSection } from "./-components/TenantOverridesSection";
import { UserOverridesSection } from "./-components/UserOverridesSection";

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

  const flag = flagsData?.flags?.find((f) => f.key === flagKey);

  const { data: tenantsData, isLoading: isLoadingTenants } = api.useQuery(
    "get",
    "/api/back-office/feature-flags/{flagKey}/tenants",
    { params: { path: { flagKey } } },
    { enabled: flag?.scope === "Tenant" }
  ) as {
    data: GetFlagTenantsResponse | undefined;
    isLoading: boolean;
  };

  const { data: usersData, isLoading: isLoadingUsers } = useQuery({
    queryKey: ["get", "/api/back-office/feature-flags/{flagKey}/users", { params: { path: { flagKey } } }],
    queryFn: async () => {
      // oxlint-disable-next-line typescript-eslint/no-explicit-any -- endpoint not yet in OpenAPI spec
      const { data } = await apiClient.GET("/api/back-office/feature-flags/{flagKey}/users" as any, {
        params: { path: { flagKey } }
      });
      return data as GetFlagUsersResponse | undefined;
    },
    enabled: flag?.scope === "User"
  });

  const isLoading =
    isLoadingFlags || (flag?.scope === "Tenant" && isLoadingTenants) || (flag?.scope === "User" && isLoadingUsers);
  const flagName = flag ? getFlagName(flag.key) : flagKey;
  const description = flag ? getFlagDescription(flag.key) || flag.description : "";

  return (
    <SidebarProvider>
      <BackOfficeSideMenu />
      <SidebarInset>
        <AppLayout
          browserTitle={flagName}
          title={
            <div className="flex items-center gap-3">
              <Link to="/feature-flags" className="text-muted-foreground hover:text-foreground">
                <ArrowLeftIcon className="size-5" aria-label={t`Back to feature flags`} />
              </Link>
              {flag && <ScopeIcon scope={flag.scope} />}
              <span>{flagName}</span>
            </div>
          }
          subtitle={flag ? description : undefined}
        >
          {isLoading ? (
            <FlagDetailSkeleton />
          ) : flag ? (
            <div className="flex flex-col gap-8">
              <FlagInfoSection flag={flag} />
              {flag.scope === "Tenant" && (
                <TenantOverridesSection
                  flagKey={flag.key}
                  flagDescription={flagName}
                  tenants={tenantsData?.tenants ?? []}
                  showBucket={flag.isAbTestEligible}
                  bucketRange={
                    flag.bucketStart != null && flag.bucketEnd != null
                      ? { bucketStart: flag.bucketStart, bucketEnd: flag.bucketEnd }
                      : null
                  }
                  isFlagActive={flag.isActive}
                />
              )}
              {flag.scope === "User" && (
                <UserOverridesSection
                  flagKey={flag.key}
                  flagDescription={flagName}
                  users={usersData?.users ?? []}
                  showBucket={flag.isAbTestEligible}
                  bucketRange={
                    flag.bucketStart != null && flag.bucketEnd != null
                      ? { bucketStart: flag.bucketStart, bucketEnd: flag.bucketEnd }
                      : null
                  }
                  isFlagActive={flag.isActive}
                />
              )}
            </div>
          ) : null}
        </AppLayout>
      </SidebarInset>
    </SidebarProvider>
  );
}

function FlagDetailSkeleton() {
  return (
    <div className="flex flex-col gap-8">
      <Skeleton className="h-20 w-full rounded-lg" />
      <div className="flex flex-col gap-4">
        <Skeleton className="h-6 w-40" />
        <Skeleton className="h-10 w-full rounded-md" />
        <Skeleton className="h-14 w-full rounded-md" />
        <Skeleton className="h-14 w-full rounded-md" />
      </div>
    </div>
  );
}
