import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Table, TableBody, TableHead, TableHeader, TableRow } from "@repo/ui/components/Table";
import { TextField } from "@repo/ui/components/TextField";
import { useQuery } from "@tanstack/react-query";
import { useEffect, useMemo, useState } from "react";

import { apiClient } from "@/shared/lib/api/client";

import type { RolloutBucketRange } from "./rolloutBucket";
import type { GetFeatureFlagUsersResponse } from "./types";

import { sortBySourceThenRolloutBucket } from "./rolloutBucket";
import { UserEmptyState } from "./UserEmptyState";
import { UserOverrideRow } from "./UserOverrideRow";

export function UserOverridesSection({
  flagKey,
  featureFlagDescription,
  showRolloutBucket,
  rolloutBucketRange
}: Readonly<{
  flagKey: string;
  featureFlagDescription: string;
  showRolloutBucket: boolean;
  rolloutBucketRange: RolloutBucketRange | null;
}>) {
  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");

  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSearch(search), 300);
    return () => clearTimeout(timer);
  }, [search]);

  const { data: usersData, isLoading } = useQuery({
    queryKey: ["get", "/api/back-office/feature-flags/{featureFlagKey}/users", { flagKey, search: debouncedSearch }],
    queryFn: async () => {
      const { data } = await apiClient.GET("/api/back-office/feature-flags/{featureFlagKey}/users", {
        params: { path: { featureFlagKey: flagKey }, query: { search: debouncedSearch } }
      });
      return data as GetFeatureFlagUsersResponse | undefined;
    },
    enabled: debouncedSearch.length > 0
  });

  const sortedUsers = useMemo(() => {
    const all = usersData?.users ?? [];
    return sortBySourceThenRolloutBucket(
      all,
      (u) => u.source,
      (u) => u.rolloutBucket,
      "enabled",
      rolloutBucketRange
    );
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
      ) : sortedUsers.length > 0 ? (
        <Table rowSize="compact" aria-label={t`Search results`}>
          <TableHeader>
            <TableRow>
              <TableHead className="w-auto">
                <Trans>Email</Trans>
              </TableHead>
              <TableHead className="hidden w-[10rem] sm:table-cell">
                <Trans>Account</Trans>
              </TableHead>
              <TableHead className="hidden w-[11rem] sm:table-cell">
                <Trans>Source</Trans>
              </TableHead>
              {showRolloutBucket && (
                <TableHead className="hidden w-[5rem] sm:table-cell">
                  <Trans>Bucket</Trans>
                </TableHead>
              )}
              <TableHead className="w-[5rem] text-right">
                <Trans>Status</Trans>
              </TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {sortedUsers.map((user) => (
              <UserOverrideRow
                key={user.userId}
                flagKey={flagKey}
                featureFlagDescription={featureFlagDescription}
                user={user}
                showRolloutBucket={showRolloutBucket}
              />
            ))}
          </TableBody>
        </Table>
      ) : (
        <UserEmptyState variant="no-results" />
      )}
    </div>
  );
}
