import { Trans } from "@lingui/react/macro";
import { Table, TableBody, TableHead, TableHeader, TableRow } from "@repo/ui/components/Table";

import type { FeatureFlagUserInfo } from "./types";

import { UserOverrideRow } from "./UserOverrideRow";

export interface UserOverridesTableProps {
  ariaLabel: string;
  users: FeatureFlagUserInfo[];
  flagKey: string;
  featureFlagDescription: string;
  showRolloutBucket: boolean;
  isFeatureFlagActive: boolean;
}

export function UserOverridesTable({
  ariaLabel,
  users,
  flagKey,
  featureFlagDescription,
  showRolloutBucket,
  isFeatureFlagActive
}: Readonly<UserOverridesTableProps>) {
  return (
    <Table rowSize="compact" aria-label={ariaLabel}>
      <TableHeader>
        <TableRow>
          <TableHead>
            <Trans>User</Trans>
          </TableHead>
          <TableHead className="hidden md:table-cell">
            <Trans>Account</Trans>
          </TableHead>
          <TableHead className="hidden lg:table-cell">
            <Trans>Role</Trans>
          </TableHead>
          <TableHead className="hidden lg:table-cell">
            <Trans>Last seen</Trans>
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
        {users.map((user) => (
          <UserOverrideRow
            key={user.id}
            flagKey={flagKey}
            featureFlagDescription={featureFlagDescription}
            user={user}
            showRolloutBucket={showRolloutBucket}
            isFeatureFlagActive={isFeatureFlagActive}
          />
        ))}
      </TableBody>
    </Table>
  );
}
