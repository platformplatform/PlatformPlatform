import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { Badge } from "@repo/ui/components/Badge";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@repo/ui/components/Table";
import { createFileRoute, Link } from "@tanstack/react-router";
import { ArrowLeftIcon } from "lucide-react";

import { api } from "@/shared/lib/api/client";

import type {
  FeatureFlagInfo,
  FlagTenantInfo,
  GetFeatureFlagsResponse,
  GetFlagTenantsResponse
} from "./-components/types";

import { FlagInfoSection } from "./-components/FlagInfoSection";
import { getFlagDescription, getFlagName } from "./-components/flagLabels";
import { ScopeIcon } from "./-components/ScopeIcon";
import { TenantOverridesSection } from "./-components/TenantOverridesSection";
import { UserOverridesSection } from "./-components/UserOverridesSection";

export const Route = createFileRoute("/back-office/feature-flags/$flagKey")({
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

  const isPlanFlag = flag?.requiredPlan != null;
  const isLoading = isLoadingFlags || (flag?.scope === "Tenant" && isLoadingTenants);
  const flagName = flag ? getFlagName(flag.key) : flagKey;
  const description = flag ? getFlagDescription(flag.key) || flag.description : "";

  return (
    <AppLayout
      maxWidth="64rem"
      browserTitle={flagName}
      title={
        <div className="flex items-center gap-3">
          <Link to="/back-office/feature-flags" className="text-muted-foreground hover:text-foreground">
            <ArrowLeftIcon className="size-5" aria-label={t`Back to feature flags`} />
          </Link>
          {flag && <ScopeIcon scope={flag.scope} className="size-6 stroke-[2.5] text-foreground" />}
          <span>{flagName}</span>
        </div>
      }
      subtitle={flag ? description : undefined}
    >
      {isLoading ? (
        <FlagDetailSkeleton />
      ) : flag ? (
        <div className="flex flex-col gap-8">
          {isPlanFlag ? <PlanFlagInfoSection flag={flag} /> : <FlagInfoSection flag={flag} />}
          {flag.scope === "Tenant" && !isPlanFlag && (
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
          {flag.scope === "Tenant" && isPlanFlag && (
            <PlanFlagTenantsSection flagKey={flag.key} tenants={tenantsData?.tenants ?? []} />
          )}
          {flag.scope === "User" && (
            <UserOverridesSection
              flagKey={flag.key}
              flagDescription={flagName}
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
  );
}

function PlanFlagInfoSection({ flag }: Readonly<{ flag: FeatureFlagInfo }>) {
  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div className="flex flex-col gap-0.5 text-sm text-muted-foreground">
          <span>
            <Trans>Name:</Trans> <span className="font-mono">{flag.key}</span>
          </span>
          <span>
            <Trans>Required plan:</Trans> <Badge variant="outline">{flag.requiredPlan}</Badge>
          </span>
        </div>
        <Badge variant={flag.isActive ? "default" : "outline"}>{flag.isActive ? t`Active` : t`Inactive`}</Badge>
      </div>
      <p className="text-sm text-muted-foreground">
        <Trans>
          This flag is managed by the subscription plan. It is automatically enabled for accounts on the required plan
          or higher.
        </Trans>
      </p>
    </div>
  );
}

function PlanFlagTenantsSection({ flagKey, tenants }: Readonly<{ flagKey: string; tenants: FlagTenantInfo[] }>) {
  return (
    <div className="flex flex-col gap-4">
      <div>
        <h3>
          <Trans>Account status</Trans>
        </h3>
        <p className="text-sm text-muted-foreground">
          <Trans>
            Accounts are automatically enabled or disabled based on their subscription plan. No manual overrides are
            available for plan-managed flags.
          </Trans>
        </p>
      </div>
      <Table rowSize="compact" aria-label={t`Accounts for ${getFlagName(flagKey)}`} className="table-fixed">
        <TableHeader>
          <TableRow>
            <TableHead className="hidden w-[10rem] lg:table-cell">
              <Trans>Account ID</Trans>
            </TableHead>
            <TableHead className="w-auto">
              <Trans>Account</Trans>
            </TableHead>
            <TableHead className="w-[5rem]">
              <Trans>Plan</Trans>
            </TableHead>
            <TableHead className="w-[6rem] text-right">
              <Trans>Status</Trans>
            </TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {tenants.map((tenant) => (
            <TableRow key={tenant.tenantId}>
              <TableCell className="hidden truncate text-muted-foreground lg:table-cell">{tenant.tenantId}</TableCell>
              <TableCell className="truncate font-medium">{tenant.tenantName}</TableCell>
              <TableCell className="text-muted-foreground">{tenant.plan}</TableCell>
              <TableCell className="text-right">
                <Badge variant={tenant.isEnabled ? "default" : "outline"}>
                  {tenant.isEnabled ? t`Enabled` : t`Disabled`}
                </Badge>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
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
