import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Table, TableBody, TableHead, TableHeader, TableRow } from "@repo/ui/components/Table";
import { TextField } from "@repo/ui/components/TextField";
import { ChevronDown } from "lucide-react";
import { useMemo, useState } from "react";

import type { BucketRange } from "./rolloutBucket";
import type { FlagUserInfo } from "./types";

import { sortBySourceThenBucket } from "./rolloutBucket";
import { UserOverrideRow } from "./UserOverrideRow";

export function UserOverridesSection({
  flagKey,
  flagDescription,
  users,
  showBucket,
  bucketRange
}: Readonly<{
  flagKey: string;
  flagDescription: string;
  users: FlagUserInfo[];
  showBucket: boolean;
  bucketRange: BucketRange | null;
}>) {
  const [search, setSearch] = useState("");

  const filtered = useMemo(() => {
    const lowerSearch = search.toLowerCase();
    return search
      ? users.filter(
          (user) =>
            user.email.toLowerCase().includes(lowerSearch) || user.tenantName.toLowerCase().includes(lowerSearch)
        )
      : users;
  }, [users, search]);

  const enabledUsers = useMemo(
    () =>
      sortBySourceThenBucket(
        filtered.filter((u) => u.isEnabled),
        (u) => u.source,
        (u) => u.userId,
        "enabled",
        bucketRange
      ),
    [filtered, bucketRange]
  );

  const disabledUsers = useMemo(
    () =>
      sortBySourceThenBucket(
        filtered.filter((u) => !u.isEnabled),
        (u) => u.source,
        (u) => u.userId,
        "disabled",
        bucketRange
      ),
    [filtered, bucketRange]
  );

  const isSearching = search.length > 0;

  return (
    <div className="flex flex-col gap-4">
      <h3>
        <Trans>User status</Trans>
      </h3>
      <TextField
        name="search"
        placeholder={t`Search by email or account name`}
        value={search}
        onChange={(value) => setSearch(value)}
        className="max-w-[20rem]"
      />
      {isSearching ? (
        <UserTable
          ariaLabel={t`Search results`}
          users={[...enabledUsers, ...disabledUsers]}
          flagKey={flagKey}
          flagDescription={flagDescription}
          showBucket={showBucket}
        />
      ) : (
        <>
          <CollapsibleUserGroup
            label={t`Enabled (${enabledUsers.length})`}
            users={enabledUsers}
            flagKey={flagKey}
            flagDescription={flagDescription}
            showBucket={showBucket}
          />
          <CollapsibleUserGroup
            label={t`Disabled (${disabledUsers.length})`}
            users={disabledUsers}
            flagKey={flagKey}
            flagDescription={flagDescription}
            showBucket={showBucket}
          />
        </>
      )}
    </div>
  );
}

interface UserTableProps {
  ariaLabel: string;
  users: FlagUserInfo[];
  flagKey: string;
  flagDescription: string;
  showBucket: boolean;
}

function UserTable({ ariaLabel, users, flagKey, flagDescription, showBucket }: Readonly<UserTableProps>) {
  return (
    <Table rowSize="compact" aria-label={ariaLabel}>
      <TableHeader>
        <TableRow>
          <TableHead>
            <Trans>Email</Trans>
          </TableHead>
          <TableHead>
            <Trans>Account</Trans>
          </TableHead>
          <TableHead className="w-[8rem]">
            <Trans>Source</Trans>
          </TableHead>
          {showBucket && (
            <TableHead className="w-[5rem]">
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
            flagDescription={flagDescription}
            user={user}
            showBucket={showBucket}
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
