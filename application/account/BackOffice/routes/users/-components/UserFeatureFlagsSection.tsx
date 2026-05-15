import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from "@repo/ui/components/Empty";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { Table, TableBody, TableHead, TableHeader, TableRow } from "@repo/ui/components/Table";

import { api } from "@/shared/lib/api/client";

import { UserFeatureFlagRow } from "./UserFeatureFlagRow";

interface UserFeatureFlagsSectionProps {
  userId: string;
}

export function UserFeatureFlagsSection({ userId }: Readonly<UserFeatureFlagsSectionProps>) {
  const { data, isLoading } = api.useQuery("get", "/api/back-office/users/{id}/feature-flags", {
    params: { path: { id: userId } }
  });

  if (isLoading) {
    return <UserFeatureFlagsSectionSkeleton />;
  }

  const flags = data?.flags ?? [];

  if (flags.length === 0) {
    return (
      <section className="flex flex-col gap-3">
        <h3>
          <Trans>Feature flags</Trans>
        </h3>
        <Empty className="border">
          <EmptyHeader>
            <EmptyTitle>
              <Trans>No feature flags</Trans>
            </EmptyTitle>
            <EmptyDescription>
              <Trans>No user-scoped feature flags are defined.</Trans>
            </EmptyDescription>
          </EmptyHeader>
        </Empty>
      </section>
    );
  }

  const hasAbTestFlag = flags.some((f) => f.isAbTestEligible);
  return (
    <section className="flex flex-col gap-3">
      <div>
        <h3>
          <Trans>Feature flags</Trans>
        </h3>
        <p className="text-sm text-muted-foreground">
          <Trans>Per-user flags. Toggle the override switch to enable or disable for this user.</Trans>
        </p>
      </div>
      <Table rowSize="compact" aria-label={t`Feature flags`} className="w-full table-fixed">
        <TableHeader>
          <TableRow>
            <TableHead>
              <Trans>Name</Trans>
            </TableHead>
            {hasAbTestFlag && (
              <TableHead className="hidden w-[8rem] text-center text-muted-foreground sm:table-cell">
                <Trans>Included at</Trans>
              </TableHead>
            )}
            <TableHead className="w-[8rem] text-center">
              <Trans>Override</Trans>
            </TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {flags.map((flag) => (
            <UserFeatureFlagRow key={flag.flagKey} userId={userId} flag={flag} showBucketColumn={hasAbTestFlag} />
          ))}
        </TableBody>
      </Table>
    </section>
  );
}

function UserFeatureFlagsSectionSkeleton() {
  return (
    <section className="flex flex-col gap-2">
      <Skeleton className="h-6 w-40" />
      <Skeleton className="h-10 w-full rounded-md" />
      <Skeleton className="h-14 w-full rounded-md" />
    </section>
  );
}
