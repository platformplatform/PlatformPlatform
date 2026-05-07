import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Badge } from "@repo/ui/components/Badge";
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from "@repo/ui/components/Empty";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@repo/ui/components/Table";
import { TenantLogo } from "@repo/ui/components/TenantLogo";

import { SmartDateTime } from "@/shared/components/SmartDateTime";
import { api } from "@/shared/lib/api/client";
import { getDeviceTypeLabel, getLoginMethodLabel } from "@/shared/lib/api/labels";
import { parseUserAgent } from "@/shared/lib/userAgent";

interface UserSessionsSectionProps {
  userId: string;
}

export function UserSessionsSection({ userId }: Readonly<UserSessionsSectionProps>) {
  const { data, isLoading } = api.useQuery("get", "/api/back-office/users/{id}/sessions", {
    params: { path: { id: userId } }
  });

  return (
    <section className="flex flex-col gap-3">
      <div className="text-sm text-muted-foreground">
        <Trans>One row per device or browser the user is signed in from. Revoked sessions cannot sign in again.</Trans>
      </div>
      {isLoading ? (
        <Skeleton className="h-24 w-full" />
      ) : !data || data.sessions.length === 0 ? (
        <Empty className="border bg-card">
          <EmptyHeader>
            <EmptyTitle>
              <Trans>No sessions</Trans>
            </EmptyTitle>
            <EmptyDescription>
              <Trans>This user has no recorded sessions.</Trans>
            </EmptyDescription>
          </EmptyHeader>
        </Empty>
      ) : (
        <Table rowSize="compact" aria-label={t`Sessions`}>
          <TableHeader>
            <TableRow>
              <TableHead>
                <Trans>Last seen</Trans>
              </TableHead>
              <TableHead>
                <Trans>Account</Trans>
              </TableHead>
              <TableHead>
                <Trans>Browser</Trans>
              </TableHead>
              <TableHead>
                <Trans>Device</Trans>
              </TableHead>
              <TableHead>
                <Trans>Method</Trans>
              </TableHead>
              <TableHead>
                <Trans>IP address</Trans>
              </TableHead>
              <TableHead>
                <Trans>Status</Trans>
              </TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {data.sessions.map((session) => {
              const { browser, os } = parseUserAgent(session.userAgent);
              return (
                <TableRow key={session.id}>
                  <TableCell>
                    <SmartDateTime date={session.lastActiveAt ?? session.createdAt} withTime={true} />
                  </TableCell>
                  <TableCell>
                    <div className="flex min-w-0 items-center gap-2">
                      <TenantLogo logoUrl={session.tenantLogoUrl} tenantName={session.tenantName} size="md" />
                      <span className="truncate">{session.tenantName}</span>
                    </div>
                  </TableCell>
                  <TableCell>
                    <div className="flex flex-col leading-tight">
                      <span>{browser}</span>
                      <span className="text-xs text-muted-foreground">{os}</span>
                    </div>
                  </TableCell>
                  <TableCell>{getDeviceTypeLabel(session.deviceType)}</TableCell>
                  <TableCell>{getLoginMethodLabel(session.loginMethod)}</TableCell>
                  <TableCell className="text-muted-foreground">{session.ipAddress}</TableCell>
                  <TableCell>
                    {session.revokedAt ? (
                      <Badge variant="outline" className="border-destructive/30 text-destructive">
                        <Trans>Revoked</Trans>
                      </Badge>
                    ) : (
                      <Badge variant="outline" className="border-emerald-500/30 text-emerald-600">
                        <Trans>Active</Trans>
                      </Badge>
                    )}
                  </TableCell>
                </TableRow>
              );
            })}
          </TableBody>
        </Table>
      )}
    </section>
  );
}
