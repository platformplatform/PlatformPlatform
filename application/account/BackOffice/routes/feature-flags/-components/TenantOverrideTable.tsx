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
    <Table rowSize="compact" aria-label={ariaLabel}>
      <TableHeader>
        <TableRow>
          <TableHead>
            <Trans>Account</Trans>
          </TableHead>
          <TableHead className="hidden md:table-cell">
            <Trans>Plan</Trans>
          </TableHead>
          <TableHead className="hidden lg:table-cell">
            <Trans>MRR</Trans>
          </TableHead>
          <TableHead className="hidden lg:table-cell">
            <Trans>Renewal</Trans>
          </TableHead>
          <TableHead className="hidden md:table-cell">
            <Trans>Status</Trans>
          </TableHead>
          <TableHead className="hidden sm:table-cell">
            <Trans>Source</Trans>
          </TableHead>
          {showRolloutBucket && (
            <TableHead className="hidden sm:table-cell">
              <Trans>Bucket</Trans>
            </TableHead>
          )}
          <TableHead className="text-right">
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
