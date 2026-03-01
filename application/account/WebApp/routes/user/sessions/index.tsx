import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogMedia,
  AlertDialogTitle
} from "@repo/ui/components/AlertDialog";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { Button } from "@repo/ui/components/Button";
import { useFormatDate } from "@repo/ui/hooks/useSmartDate";
import { useQueryClient } from "@tanstack/react-query";
import { createFileRoute } from "@tanstack/react-router";
import { InfoIcon, LoaderIcon, LogOutIcon, MonitorIcon, SmartphoneIcon, TabletIcon } from "lucide-react";
import { useState } from "react";
import { toast } from "sonner";
import { SmartDate } from "@/shared/components/SmartDate";
import { api, type components, DeviceType, LoginMethod } from "@/shared/lib/api/client";

export const Route = createFileRoute("/user/sessions/")({
  component: SessionsPage
});

type UserSessionInfo = components["schemas"]["UserSessionInfo"];

function parseUserAgent(userAgent: string): { browser: string; os: string } {
  const browserPatterns = [
    { pattern: /Edg\/[\d.]+/, name: "Edge" },
    { pattern: /Chrome\/[\d.]+/, name: "Chrome" },
    { pattern: /Firefox\/[\d.]+/, name: "Firefox" },
    { pattern: /Safari\/[\d.]+/, name: "Safari" },
    { pattern: /OPR\/[\d.]+/, name: "Opera" }
  ];

  const osPatterns = [
    { pattern: /Windows NT 10/, name: "Windows" },
    { pattern: /Windows NT/, name: "Windows" },
    { pattern: /Mac OS X/, name: "macOS" },
    { pattern: /Linux/, name: "Linux" },
    { pattern: /Android/, name: "Android" },
    { pattern: /iPhone|iPad/, name: "iOS" }
  ];

  let browser = t`Unknown`;
  let os = t`Unknown`;

  for (const { pattern, name } of browserPatterns) {
    if (pattern.test(userAgent)) {
      browser = name;
      break;
    }
  }

  for (const { pattern, name } of osPatterns) {
    if (pattern.test(userAgent)) {
      os = name;
      break;
    }
  }

  return { browser, os };
}

function getDeviceTypeLabel(deviceType: UserSessionInfo["deviceType"]): string {
  switch (deviceType) {
    case DeviceType.Mobile:
      return t`Mobile`;
    case DeviceType.Tablet:
      return t`Tablet`;
    case DeviceType.Desktop:
      return t`Desktop`;
    default:
      return t`Unknown`;
  }
}

function getDeviceIcon(deviceType: UserSessionInfo["deviceType"]) {
  switch (deviceType) {
    case DeviceType.Mobile:
      return SmartphoneIcon;
    case DeviceType.Tablet:
      return TabletIcon;
    case DeviceType.Desktop:
      return MonitorIcon;
    default:
      return MonitorIcon;
  }
}

function getLoginMethodLabel(loginMethod: UserSessionInfo["loginMethod"]): string {
  switch (loginMethod) {
    case LoginMethod.OneTimePassword:
      return t`One-time password`;
    case LoginMethod.Google:
      return t`Google`;
    default:
      return t`Unknown`;
  }
}

function SessionCard({
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
              <h4 className="font-medium text-lg">{browser}</h4>
              <p className="text-muted-foreground">
                {os} · {deviceType}
              </p>
            </div>
          </div>

          {isCurrent ? (
            <span className="shrink-0 rounded-full bg-success px-3 py-1 font-medium text-sm text-success-foreground">
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

function RevokeSessionDialog({
  isOpen,
  onOpenChange,
  onRevoke
}: Readonly<{
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
  onRevoke: () => void;
}>) {
  return (
    <AlertDialog open={isOpen} onOpenChange={onOpenChange}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogMedia className="bg-destructive/10">
            <LogOutIcon className="text-destructive" />
          </AlertDialogMedia>
          <AlertDialogTitle>{t`Revoke session`}</AlertDialogTitle>
          <AlertDialogDescription>
            <Trans>
              Are you sure you want to revoke this session? The device will be logged out and will need to log in again.
            </Trans>
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel variant="secondary">
            <Trans>Cancel</Trans>
          </AlertDialogCancel>
          <AlertDialogAction variant="destructive" onClick={onRevoke}>
            <Trans>Revoke</Trans>
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}

function SessionsPage() {
  const [selectedSession, setSelectedSession] = useState<UserSessionInfo | null>(null);
  const [isRevokeDialogOpen, setIsRevokeDialogOpen] = useState(false);
  const [hasRevokedSession, setHasRevokedSession] = useState(false);
  const [revokingSessionIds, setRevokingSessionIds] = useState<Set<string>>(new Set());
  const queryClient = useQueryClient();

  const { data, isLoading } = api.useQuery("get", "/api/account/authentication/sessions", {});

  const sessions = data?.sessions ?? [];
  const currentSession = sessions.find((s) => s.isCurrent);
  const otherSessions = sessions.filter((s) => !s.isCurrent);
  const currentTenantName = currentSession?.tenantName;

  const revokeSessionMutation = api.useMutation("delete", "/api/account/authentication/sessions/{id}", {
    onSuccess: () => {
      if (selectedSession) {
        setRevokingSessionIds((prev) => {
          const next = new Set(prev);
          next.delete(selectedSession.id);
          return next;
        });
      }
      toast.success(t`Session revoked successfully`);
      queryClient.invalidateQueries({ queryKey: ["get", "/api/account/authentication/sessions"] });

      setIsRevokeDialogOpen(false);
      setSelectedSession(null);
      setHasRevokedSession(true);
    },
    onError: () => {
      if (selectedSession) {
        setRevokingSessionIds((prev) => {
          const next = new Set(prev);
          next.delete(selectedSession.id);
          return next;
        });
      }
    }
  });

  const handleRevokeSession = (session: UserSessionInfo) => {
    setSelectedSession(session);
    setIsRevokeDialogOpen(true);
  };

  const handleConfirmRevoke = () => {
    if (selectedSession) {
      setRevokingSessionIds((prev) => new Set(prev).add(selectedSession.id));
      setIsRevokeDialogOpen(false);
      revokeSessionMutation.mutate({ params: { path: { id: selectedSession.id } } });
    }
  };

  if (isLoading) {
    return (
      <AppLayout variant="center" maxWidth="64rem" title={t`User sessions`}>
        <div className="flex flex-1 items-center justify-center py-12">
          <LoaderIcon className="size-8 animate-spin text-muted-foreground" />
        </div>
      </AppLayout>
    );
  }

  return (
    <>
      <AppLayout
        variant="center"
        maxWidth="64rem"
        balanceWidth="16rem"
        title={t`User sessions`}
        subtitle={t`Devices that have logged into your account. Revoke any sessions you do not recognize.`}
      >
        <div className="flex flex-col gap-6 pt-8">
          {hasRevokedSession && (
            <div className="flex items-center gap-3 rounded-lg border border-border bg-muted/50 p-4">
              <InfoIcon className="size-4 shrink-0 text-info" />
              <p className="text-sm">
                <Trans>It may take up to 5 minutes before the device is logged out.</Trans>
              </p>
            </div>
          )}

          {currentSession && (
            <SessionCard
              session={currentSession}
              isRevoking={false}
              onRevoke={handleRevokeSession}
              isCurrent={true}
              showAccountName={false}
            />
          )}

          {otherSessions.map((session) => (
            <SessionCard
              key={session.id}
              session={session}
              isRevoking={revokingSessionIds.has(session.id)}
              onRevoke={handleRevokeSession}
              isCurrent={false}
              showAccountName={session.tenantName !== currentTenantName}
            />
          ))}

          {sessions.length === 0 && (
            <p className="py-8 text-center text-muted-foreground">
              <Trans>No active sessions found.</Trans>
            </p>
          )}
        </div>
      </AppLayout>

      <RevokeSessionDialog
        isOpen={isRevokeDialogOpen}
        onOpenChange={setIsRevokeDialogOpen}
        onRevoke={handleConfirmRevoke}
      />
    </>
  );
}
