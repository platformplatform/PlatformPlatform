import type { RowKey } from "@repo/ui/components/Table";

import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from "@repo/ui/components/Empty";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@repo/ui/components/Table";
import { TenantLogo } from "@repo/ui/components/TenantLogo";
import { Link, useNavigate } from "@tanstack/react-router";
import { ArrowRightIcon, BuildingIcon } from "lucide-react";
import { useCallback } from "react";

import { SmartDateTime } from "@/shared/components/SmartDateTime";
import { api, SortableTenantProperties } from "@/shared/lib/api/client";

import { DashboardCardShell } from "./DashboardCardShell";

function getOwnerDisplayName(owner: { firstName: string | null; lastName: string | null; email: string }): string {
  const fullName = [owner.firstName, owner.lastName].filter((part) => part != null && part.trim() !== "").join(" ");
  return fullName !== "" ? fullName : owner.email;
}

export function DashboardRecentSignupsCard() {
  const navigate = useNavigate();
  const { data, isLoading } = api.useQuery("get", "/api/back-office/dashboard/recent-signups", {
    params: { query: { Limit: 6 } }
  });

  const signups = data?.signups ?? [];

  const handleActivate = useCallback(
    (key: RowKey) => {
      navigate({ to: "/accounts/$tenantId", params: { tenantId: String(key) } });
    },
    [navigate]
  );

  return (
    <DashboardCardShell
      title={<Trans>Recent signups</Trans>}
      action={
        <Link
          to="/accounts"
          search={{ orderBy: SortableTenantProperties.CreatedAt }}
          className="flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
        >
          <Trans>View all</Trans>
          <ArrowRightIcon className="size-3.5" aria-hidden="true" />
        </Link>
      }
    >
      {isLoading ? (
        <div className="flex flex-col gap-3">
          {[0, 1, 2, 3, 4, 5].map((index) => (
            <Skeleton key={index} className="h-12 w-full" />
          ))}
        </div>
      ) : signups.length === 0 ? (
        <Empty className="border bg-card">
          <EmptyHeader>
            <BuildingIcon className="size-6 text-muted-foreground" aria-hidden="true" />
            <EmptyTitle>
              <Trans>No recent signups</Trans>
            </EmptyTitle>
            <EmptyDescription>
              <Trans>New accounts will appear here as they sign up.</Trans>
            </EmptyDescription>
          </EmptyHeader>
        </Empty>
      ) : (
        <Table
          rowSize="compact"
          aria-label={t`Recent signups`}
          selectionMode="single"
          onActivate={handleActivate}
          containerClassName="border-0 bg-transparent"
        >
          <TableHeader>
            <TableRow>
              <TableHead>
                <Trans>Account</Trans>
              </TableHead>
              <TableHead className="hidden md:table-cell">
                <Trans>Owner</Trans>
              </TableHead>
              <TableHead className="text-right">
                <Trans>Signed up</Trans>
              </TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {signups.map((signup) => (
              <TableRow key={signup.tenantId} rowKey={String(signup.tenantId)}>
                <TableCell>
                  <div className="flex min-w-0 items-center gap-2">
                    <TenantLogo
                      logoUrl={signup.tenantLogoUrl}
                      tenantName={signup.name}
                      size="md"
                      className="size-8 shrink-0"
                    />
                    <span className="truncate text-sm font-medium">{signup.name}</span>
                  </div>
                </TableCell>
                <TableCell className="hidden md:table-cell">
                  {signup.owner ? (
                    <span className="truncate text-sm">{getOwnerDisplayName(signup.owner)}</span>
                  ) : (
                    <span className="text-muted-foreground">—</span>
                  )}
                </TableCell>
                <TableCell className="text-right">
                  <SmartDateTime date={signup.createdAt} className="text-xs whitespace-nowrap text-muted-foreground" />
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}
    </DashboardCardShell>
  );
}
