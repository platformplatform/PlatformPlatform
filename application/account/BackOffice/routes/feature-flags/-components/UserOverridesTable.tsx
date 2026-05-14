import { Trans } from "@lingui/react/macro";
import { Table, TableBody, TableHead, TableHeader, TableRow } from "@repo/ui/components/Table";

import type { StateFilter } from "./stateFilter";
import type { FeatureFlagUserInfo } from "./types";

import { UserOverrideRow } from "./UserOverrideRow";

export interface UserOverridesTableProps {
  ariaLabel: string;
  users: FeatureFlagUserInfo[];
  flagKey: string;
  featureFlagDescription: string;
  showRolloutBucket: boolean;
  isFeatureFlagActive: boolean;
  stateFilter: StateFilter | undefined;
  hasOverrideFilter: boolean;
}

export function UserOverridesTable({
  ariaLabel,
  users,
  flagKey,
  featureFlagDescription,
  showRolloutBucket,
  isFeatureFlagActive,
  stateFilter,
  hasOverrideFilter
}: Readonly<UserOverridesTableProps>) {
  return (
    <Table rowSize="compact" aria-label={ariaLabel} className="w-full table-fixed">
      <TableHeader>
        <TableRow>
          <TableHead>
            <Trans>User</Trans>
          </TableHead>
          <TableHead className="hidden md:table-cell">
            <Trans>Account</Trans>
          </TableHead>
          <TableHead className="hidden w-[6rem] lg:table-cell">
            <Trans>Role</Trans>
          </TableHead>
          <TableHead className="hidden w-[8.5rem] lg:table-cell">
            <Trans>Last seen</Trans>
          </TableHead>
          {showRolloutBucket && (
            <TableHead className="hidden w-[7rem] text-center lg:table-cell">
              <Trans>Included at</Trans>
            </TableHead>
          )}
          <TableHead className="w-[5rem] text-right">
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
            stateFilter={stateFilter}
            hasOverrideFilter={hasOverrideFilter}
          />
        ))}
      </TableBody>
    </Table>
  );
}
