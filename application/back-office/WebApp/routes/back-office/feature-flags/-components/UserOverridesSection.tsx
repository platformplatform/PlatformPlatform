import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Table, TableBody, TableHead, TableHeader, TableRow } from "@repo/ui/components/Table";
import { TextField } from "@repo/ui/components/TextField";
import { useQuery } from "@tanstack/react-query";
import { ChevronDown } from "lucide-react";
import { useEffect, useMemo, useState } from "react";

import { apiClient } from "@/shared/lib/api/client";

import type { RolloutBucketRange } from "./rolloutBucket";
import type { FeatureFlagUserInfo, GetFeatureFlagUsersResponse } from "./types";

import { sortBySourceThenRolloutBucket } from "./rolloutBucket";
import { UserEmptyState } from "./UserEmptyState";
import { UserOverrideRow } from "./UserOverrideRow";

export function UserOverridesSection({
  flagKey,
  featureFlagDescription,
  showRolloutBucket,
  rolloutBucketRange,
  isFeatureFlagActive
}: Readonly<{
  flagKey: string;
  featureFlagDescription: string;
  showRolloutBucket: boolean;
  rolloutBucketRange: RolloutBucketRange | null;
  isFeatureFlagActive: boolean;
}>) {
  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");

  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSearch(search), 300);
    return () => clearTimeout(timer);
  }, [search]);

  const { data: usersData, isLoading } = useQuery({
    queryKey: ["get", "/api/back-office/feature-flags/{flagKey}/users", { flagKey, search: debouncedSearch }],
    queryFn: async () => {
      // oxlint-disable-next-line typescript-eslint/no-explicit-any -- endpoint not yet in OpenAPI spec
      const { data } = await apiClient.GET("/api/back-office/feature-flags/{flagKey}/users" as any, {
        params: { path: { flagKey }, query: { search: debouncedSearch } }
      });
      return data as GetFeatureFlagUsersResponse | undefined;
    },
    enabled: debouncedSearch.length > 0
  });

  const { enabledUsers, disabledUsers } = useMemo(() => {
    const all = usersData?.users ?? [];
    const enabled = all.filter((u) => u.isEnabled);
    const disabled = all.filter((u) => !u.isEnabled);
    return {
      enabledUsers: sortBySourceThenRolloutBucket(
        enabled,
        (u) => u.source,
        (u) => u.rolloutBucket,
        "enabled",
        rolloutBucketRange
      ),
      disabledUsers: sortBySourceThenRolloutBucket(
        disabled,
        (u) => u.source,
        (u) => u.rolloutBucket,
        "disabled",
        rolloutBucketRange
      )
    };
  }, [usersData?.users, rolloutBucketRange]);

  const hasSearched = debouncedSearch.length > 0;

  return (
    <div className="flex flex-col gap-4">
      <div>
        <h3>
          <Trans>User status</Trans>
        </h3>
        <p className="text-sm text-muted-foreground">
          {showRolloutBucket ? (
            <Trans>
              Users are automatically included based on their rollout bucket. Use overrides to manually include or
              exclude specific users.
            </Trans>
          ) : (
            <Trans>Search for users by email and toggle the override switch to enable this feature.</Trans>
          )}
        </p>
      </div>
      <TextField
        name="search"
        placeholder={t`Search by email`}
        value={search}
        onChange={(value) => setSearch(value)}
        className="max-w-[20rem]"
      />
      {!hasSearched ? (
        <UserEmptyState variant="no-users" />
      ) : isLoading ? (
        <UserEmptyState variant="loading" />
      ) : enabledUsers.length + disabledUsers.length > 0 ? (
        <>
          <CollapsibleUserGroup
            label={t`Enabled (${enabledUsers.length})`}
            users={enabledUsers}
            flagKey={flagKey}
            featureFlagDescription={featureFlagDescription}
            showRolloutBucket={showRolloutBucket}
            isFeatureFlagActive={isFeatureFlagActive}
          />
          <CollapsibleUserGroup
            label={t`Disabled (${disabledUsers.length})`}
            users={disabledUsers}
            flagKey={flagKey}
            featureFlagDescription={featureFlagDescription}
            showRolloutBucket={showRolloutBucket}
            isFeatureFlagActive={isFeatureFlagActive}
          />
        </>
      ) : (
        <UserEmptyState variant="no-results" />
      )}
    </div>
  );
}

interface UserTableProps {
  ariaLabel: string;
  users: FeatureFlagUserInfo[];
  flagKey: string;
  featureFlagDescription: string;
  showRolloutBucket: boolean;
  isFeatureFlagActive: boolean;
}

function UserTable({
  ariaLabel,
  users,
  flagKey,
  featureFlagDescription,
  showRolloutBucket,
  isFeatureFlagActive
}: Readonly<UserTableProps>) {
  return (
    <Table rowSize="compact" aria-label={ariaLabel} className="table-fixed">
      <TableHeader>
        <TableRow>
          <TableHead className="w-auto">
            <Trans>Email</Trans>
          </TableHead>
          <TableHead className="w-[10rem]">
            <Trans>Account</Trans>
          </TableHead>
          <TableHead className="hidden w-[8rem] sm:table-cell">
            <Trans>Source</Trans>
          </TableHead>
          {showRolloutBucket && (
            <TableHead className="hidden w-[5rem] sm:table-cell">
              <Trans>Bucket</Trans>
            </TableHead>
          )}
          <TableHead className="w-[7rem] text-right">
            <Trans>Override</Trans>
          </TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {users.map((user) => (
          <UserOverrideRow
            key={user.userId}
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

function CollapsibleUserGroup({
  label,
  ...tableProps
}: Readonly<{ label: string } & Omit<UserTableProps, "ariaLabel">>) {
  const [isOpen, setIsOpen] = useState(true);

  return (
    <div className="flex flex-col gap-1">
      <button
        type="button"
        className="flex cursor-pointer items-center gap-1 text-left"
        onClick={() => setIsOpen((prev) => !prev)}
        aria-expanded={isOpen}
      >
        <ChevronDown
          className={`size-4 text-muted-foreground transition ${isOpen ? "" : "-rotate-90"}`}
          aria-hidden={true}
        />
        <h4 className="text-muted-foreground">{label}</h4>
      </button>
      {isOpen && <UserTable ariaLabel={label} {...tableProps} />}
    </div>
  );
}
