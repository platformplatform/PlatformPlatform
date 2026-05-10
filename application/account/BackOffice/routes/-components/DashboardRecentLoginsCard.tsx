import type { RowKey } from "@repo/ui/components/Table";

import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Avatar, AvatarFallback, AvatarImage } from "@repo/ui/components/Avatar";
import { Badge } from "@repo/ui/components/Badge";
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from "@repo/ui/components/Empty";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@repo/ui/components/Table";
import { TenantLogo } from "@repo/ui/components/TenantLogo";
import { Link, useNavigate } from "@tanstack/react-router";
import { ArrowRightIcon, KeyRoundIcon } from "lucide-react";
import { useCallback } from "react";

import { SmartDateTime } from "@/shared/components/SmartDateTime";
import { api } from "@/shared/lib/api/client";
import { getLoginMethodLabel } from "@/shared/lib/api/labels";

import { getUserDisplayName, getUserInitials } from "../users/-components/userDisplay";
import { DashboardCardShell } from "./DashboardCardShell";

export function DashboardRecentLoginsCard() {
  const navigate = useNavigate();
  const { data, isLoading } = api.useQuery("get", "/api/back-office/dashboard/recent-logins", {
    params: { query: { Limit: 6 } }
  });

  const logins = data?.logins ?? [];

  const handleActivate = useCallback(
    (key: RowKey) => {
      const userId = String(key).split("|")[0];
      // Logins not tied to a known user (rare in practice, but possible if the user was deleted) leave the
      // synthetic key prefix empty — skip navigation in that case to avoid landing on a 404 detail page.
      if (userId !== "") {
        navigate({ to: "/users/$userId", params: { userId } });
      }
    },
    [navigate]
  );

  return (
    <DashboardCardShell
      title={<Trans>Recent logins</Trans>}
      action={
        <Link to="/users" className="flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground">
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
      ) : logins.length === 0 ? (
        <Empty className="border bg-card">
          <EmptyHeader>
            <KeyRoundIcon className="size-6 text-muted-foreground" aria-hidden="true" />
            <EmptyTitle>
              <Trans>No recent logins</Trans>
            </EmptyTitle>
            <EmptyDescription>
              <Trans>Successful logins will appear here as users sign in.</Trans>
            </EmptyDescription>
          </EmptyHeader>
        </Empty>
      ) : (
        <Table
          rowSize="compact"
          aria-label={t`Recent logins`}
          selectionMode="single"
          onActivate={handleActivate}
          containerClassName="border-0 bg-transparent"
        >
          <TableHeader>
            <TableRow>
              <TableHead>
                <Trans>User</Trans>
              </TableHead>
              <TableHead className="hidden md:table-cell">
                <Trans>Account</Trans>
              </TableHead>
              <TableHead>
                <Trans>Method</Trans>
              </TableHead>
              <TableHead className="text-right">
                <Trans>When</Trans>
              </TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {logins.map((login, index) => (
              <TableRow
                key={`${login.userId ?? ""}|${login.occurredAt}|${index}`}
                rowKey={`${login.userId ?? ""}|${login.occurredAt}|${index}`}
              >
                <TableCell>
                  <div className="flex min-w-0 items-center gap-2">
                    <Avatar size="default" className="size-8 shrink-0">
                      {login.avatarUrl && (
                        <AvatarImage
                          src={login.avatarUrl}
                          alt={getUserDisplayName(login.firstName, login.lastName, login.email)}
                        />
                      )}
                      <AvatarFallback>{getUserInitials(login.firstName, login.lastName, login.email)}</AvatarFallback>
                    </Avatar>
                    <span className="truncate text-sm font-medium">
                      {getUserDisplayName(login.firstName, login.lastName, login.email)}
                    </span>
                  </div>
                </TableCell>
                <TableCell className="hidden md:table-cell">
                  {login.tenantName ? (
                    <div className="flex min-w-0 items-center gap-2">
                      <TenantLogo
                        logoUrl={login.tenantLogoUrl}
                        tenantName={login.tenantName}
                        size="md"
                        className="size-8 shrink-0"
                      />
                      <span className="truncate text-sm">{login.tenantName}</span>
                    </div>
                  ) : (
                    <span className="text-muted-foreground">—</span>
                  )}
                </TableCell>
                <TableCell>
                  <Badge variant="outline">{getLoginMethodLabel(login.method)}</Badge>
                </TableCell>
                <TableCell className="text-right">
                  <SmartDateTime date={login.occurredAt} className="text-xs whitespace-nowrap text-muted-foreground" />
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}
    </DashboardCardShell>
  );
}
