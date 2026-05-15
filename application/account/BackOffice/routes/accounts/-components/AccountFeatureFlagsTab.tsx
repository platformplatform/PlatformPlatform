import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from "@repo/ui/components/Empty";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { Table, TableBody, TableHead, TableHeader, TableRow } from "@repo/ui/components/Table";
import { useMemo } from "react";

import type { components } from "@/shared/lib/api/client";

import { api } from "@/shared/lib/api/client";

import { AccountFeatureFlagRow } from "./AccountFeatureFlagRow";

type TenantFeatureFlagInfo = components["schemas"]["TenantFeatureFlagInfo"];

interface AccountFeatureFlagsTabProps {
  tenantId: string;
}

export function AccountFeatureFlagsTab({ tenantId }: Readonly<AccountFeatureFlagsTabProps>) {
  const { data, isLoading } = api.useQuery("get", "/api/back-office/tenants/{id}/feature-flags", {
    params: { path: { id: tenantId } }
  });

  const { accountFlags, planFlags } = useMemo(() => {
    const flags = data?.flags ?? [];
    return {
      accountFlags: flags.filter((f) => f.requiredPlan == null),
      planFlags: flags.filter((f) => f.requiredPlan != null)
    };
  }, [data?.flags]);

  if (isLoading) {
    return <FeatureFlagsTabSkeleton />;
  }

  if (accountFlags.length === 0 && planFlags.length === 0) {
    return (
      <Empty className="border">
        <EmptyHeader>
          <EmptyTitle>
            <Trans>No feature flags</Trans>
          </EmptyTitle>
          <EmptyDescription>
            <Trans>No tenant-scoped feature flags are defined for this account.</Trans>
          </EmptyDescription>
        </EmptyHeader>
      </Empty>
    );
  }

  return (
    <div className="flex flex-col gap-8">
      {accountFlags.length > 0 && (
        <FeatureFlagGroup
          tenantId={tenantId}
          flags={accountFlags}
          title={t`Account flags`}
          description={t`Per-account flags. Toggle the override switch to enable or disable for this account.`}
          isPlanGroup={false}
        />
      )}
      {planFlags.length > 0 && (
        <FeatureFlagGroup
          tenantId={tenantId}
          flags={planFlags}
          title={t`Plan flags`}
          description={t`Gated by the account's subscription plan. Read-only.`}
          isPlanGroup={true}
        />
      )}
    </div>
  );
}

function FeatureFlagGroup({
  tenantId,
  flags,
  title,
  description,
  isPlanGroup
}: Readonly<{
  tenantId: string;
  flags: TenantFeatureFlagInfo[];
  title: string;
  description: string;
  isPlanGroup: boolean;
}>) {
  const hasAbTestFlag = flags.some((f) => f.isAbTestEligible);
  return (
    <div className="flex flex-col gap-2">
      <h3>{title}</h3>
      <p className="text-sm text-muted-foreground">{description}</p>
      <Table rowSize="compact" aria-label={title} className="w-full table-fixed">
        <TableHeader>
          <TableRow>
            <TableHead>
              <Trans>Name</Trans>
            </TableHead>
            {isPlanGroup && (
              <TableHead className="hidden w-[8rem] text-center sm:table-cell">
                <Trans>Required plan</Trans>
              </TableHead>
            )}
            {!isPlanGroup && hasAbTestFlag && (
              <TableHead className="hidden w-[8rem] text-center sm:table-cell">
                <Trans>Included at</Trans>
              </TableHead>
            )}
            <TableHead className="w-[8rem] text-center">
              {isPlanGroup ? <Trans>Status</Trans> : <Trans>Override</Trans>}
            </TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {flags.map((flag) => (
            <AccountFeatureFlagRow
              key={flag.flagKey}
              tenantId={tenantId}
              flag={flag}
              isPlanGroup={isPlanGroup}
              showBucketColumn={!isPlanGroup && hasAbTestFlag}
            />
          ))}
        </TableBody>
      </Table>
    </div>
  );
}

function FeatureFlagsTabSkeleton() {
  return (
    <div className="flex flex-col gap-2">
      <Skeleton className="h-10 w-full rounded-md" />
      <Skeleton className="h-14 w-full rounded-md" />
      <Skeleton className="h-14 w-full rounded-md" />
    </div>
  );
}
