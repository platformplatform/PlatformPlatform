import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { useFormatDate } from "@repo/ui/hooks/useSmartDate";

import { SmartDate } from "@/shared/components/SmartDate";

import {
  getDeviceIcon,
  getDeviceTypeLabel,
  getLoginMethodLabel,
  parseUserAgent,
  type UserSessionInfo
} from "./sessionUtils";

export function SessionCard({
  session,
  isRevoking,
  onRevoke,
  isCurrent,
  showAccountName
}: Readonly<{
  session: UserSessionInfo;
  isRevoking: boolean;
  onRevoke: (session: UserSessionInfo) => void;
  isCurrent: boolean;
  showAccountName: boolean;
}>) {
  const formatDate = useFormatDate();
  const { browser, os } = parseUserAgent(session.userAgent);
  const deviceType = getDeviceTypeLabel(session.deviceType);
  const DeviceIcon = getDeviceIcon(session.deviceType);

  return (
    <div className="rounded-xl border border-border p-6">
      <div className="flex flex-col gap-5">
        <div className="flex items-start justify-between gap-4">
          <div className="flex gap-4">
            <div className="flex size-12 shrink-0 items-center justify-center rounded-lg bg-muted">
              <DeviceIcon className="size-6 text-muted-foreground" />
            </div>
            <div>
              <h4 className="text-lg font-medium">{browser}</h4>
              <p className="text-muted-foreground">
                {os} · {deviceType}
              </p>
            </div>
          </div>

          {isCurrent ? (
            <span className="shrink-0 rounded-full bg-success px-3 py-1 text-sm font-medium text-success-foreground">
              <Trans>This device</Trans>
            </span>
          ) : (
            <Button variant="secondary" onClick={() => onRevoke(session)} disabled={isRevoking} className="shrink-0">
              {isRevoking ? <Trans>Revoking...</Trans> : <Trans>Revoke</Trans>}
            </Button>
          )}
        </div>

        <div className="grid grid-cols-1 gap-3 text-sm md:flex md:justify-between">
          {showAccountName && (
            <div className="flex justify-between md:flex-col md:gap-1">
              <span className="text-muted-foreground">
                <Trans>Account</Trans>
              </span>
              <span>{session.tenantName}</span>
            </div>
          )}
          <div className="flex justify-between md:flex-col md:gap-1">
            <span className="text-muted-foreground">
              <Trans>Login method</Trans>
            </span>
            <span>{getLoginMethodLabel(session.loginMethod)}</span>
          </div>
          <div className="flex justify-between md:flex-col md:gap-1">
            <span className="text-muted-foreground">
              <Trans>IP address</Trans>
            </span>
            <span>{session.ipAddress}</span>
          </div>
          <div className="flex justify-between md:flex-col md:gap-1">
            <span className="text-muted-foreground">
              <Trans>Last active</Trans>
            </span>
            <span>
              <SmartDate date={session.lastActivityAt} />
            </span>
          </div>
          <div className="flex justify-between md:flex-col md:gap-1">
            <span className="text-muted-foreground">
              <Trans>Created</Trans>
            </span>
            <span>{formatDate(session.createdAt, true)}</span>
          </div>
        </div>
      </div>
    </div>
  );
}
