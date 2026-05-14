import { Trans } from "@lingui/react/macro";
import { Table, TableBody, TableHead, TableHeader, TableRow } from "@repo/ui/components/Table";

import type { FeatureFlagTenantInfo } from "./types";

import { TenantOverrideRow } from "./TenantOverrideRow";

export interface TenantOverrideTableProps {
  ariaLabel: string;
  tenants: FeatureFlagTenantInfo[];
  flagKey: string;
  featureFlagDescription: string;
  showRolloutBucket: boolean;
  isFeatureFlagActive: boolean;
}

export function TenantOverrideTable({
  ariaLabel,
  tenants,
  flagKey,
  featureFlagDescription,
  showRolloutBucket,
  isFeatureFlagActive
}: Readonly<TenantOverrideTableProps>) {
  return (
    <Table rowSize="compact" aria-label={ariaLabel} className="w-full table-fixed">
      <TableHeader>
        <TableRow>
          <TableHead>
            <Trans>Account</Trans>
          </TableHead>
          <TableHead className="hidden w-[6rem] md:table-cell">
            <Trans>Plan</Trans>
          </TableHead>
          <TableHead className="hidden w-[6rem] lg:table-cell">
            <Trans>MRR</Trans>
          </TableHead>
          <TableHead className="hidden w-[7rem] lg:table-cell">
            <Trans>Renewal</Trans>
          </TableHead>
          <TableHead className="hidden w-[6rem] md:table-cell">
            <Trans>Status</Trans>
          </TableHead>
          <TableHead className="hidden w-[7rem] lg:table-cell">
            <Trans>Source</Trans>
          </TableHead>
          {showRolloutBucket && (
            <TableHead className="hidden w-[5rem] lg:table-cell">
              <Trans>Bucket</Trans>
            </TableHead>
          )}
          <TableHead className="w-[5rem] text-right">
            <Trans>Override</Trans>
          </TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {tenants.map((tenant) => (
          <TenantOverrideRow
            key={tenant.id}
            flagKey={flagKey}
            featureFlagDescription={featureFlagDescription}
            tenant={tenant}
            showRolloutBucket={showRolloutBucket}
            isFeatureFlagActive={isFeatureFlagActive}
          />
        ))}
      </TableBody>
    </Table>
  );
}
