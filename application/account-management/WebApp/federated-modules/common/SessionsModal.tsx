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
  AlertDialogTitle
} from "@repo/ui/components/AlertDialog";
import { Badge } from "@repo/ui/components/Badge";
import { Button } from "@repo/ui/components/Button";
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle
} from "@repo/ui/components/Dialog";
import { toastQueue } from "@repo/ui/components/Toast";
import { formatDate } from "@repo/utils/date/formatDate";
import { useQueryClient } from "@tanstack/react-query";
import { InfoIcon, LaptopIcon, LoaderIcon, MonitorIcon, SmartphoneIcon, TabletIcon } from "lucide-react";
import { useState } from "react";
import { SmartDate } from "@/shared/components/SmartDate";
import { api, type components, DeviceType } from "@/shared/lib/api/client";

type UserSessionInfo = components["schemas"]["UserSessionInfo"];

type SessionsModalProps = {
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
};

function getDeviceIcon(deviceType: UserSessionInfo["deviceType"]) {
  switch (deviceType) {
    case DeviceType.Mobile:
      return <SmartphoneIcon className="h-6 w-6 text-muted-foreground" aria-hidden="true" />;
    case DeviceType.Tablet:
      return <TabletIcon className="h-6 w-6 text-muted-foreground" aria-hidden="true" />;
    case DeviceType.Desktop:
      return <LaptopIcon className="h-6 w-6 text-muted-foreground" aria-hidden="true" />;
    default:
      return <MonitorIcon className="h-6 w-6 text-muted-foreground" aria-hidden="true" />;
  }
}

function parseUserAgent(userAgent: string): string {
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
    { pattern: /Mac OS X/, name: "Mac" },
    { pattern: /Linux/, name: "Linux" },
    { pattern: /Android/, name: "Android" },
    { pattern: /iPhone|iPad/, name: "iOS" }
  ];

  let browser = t`Unknown browser`;
  let os = t`Unknown OS`;

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

  return t`${browser} on ${os}`;
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

function SessionCard({
  session,
  isRevoking,
  onRevoke
}: Readonly<{ session: UserSessionInfo; isRevoking: boolean; onRevoke: (session: UserSessionInfo) => void }>) {
  const deviceLabel = getDeviceTypeLabel(session.deviceType);
  const browserInfo = parseUserAgent(session.userAgent);

  return (
    <div className="flex flex-col gap-3 rounded-lg border border-border bg-card p-4 sm:flex-row sm:items-start sm:justify-between">
      <div className="flex items-start gap-4">
        <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-muted">
          {getDeviceIcon(session.deviceType)}
        </div>
        <div className="flex flex-col gap-1">
          <div className="flex flex-wrap items-center gap-2">
            <span className="font-medium text-foreground">
              {deviceLabel} ({browserInfo})
            </span>
            {session.isCurrent && (
              <Badge variant="secondary" className="bg-success text-success-foreground">
                <Trans>Current session</Trans>
              </Badge>
            )}
          </div>
          <div className="flex flex-col gap-0.5 text-muted-foreground text-sm">
            <span>
              <Trans>Account:</Trans> {session.tenantName || <Trans>Unnamed account</Trans>}
            </span>
            <span>
              <Trans>IP:</Trans> {session.ipAddress}
            </span>
            <span>
              <Trans>Last active:</Trans> <SmartDate date={session.lastActivityAt} />
            </span>
            <span>
              <Trans>Created:</Trans> {formatDate(session.createdAt)}
            </span>
          </div>
        </div>
      </div>
      {!session.isCurrent && (
        <Button
          variant="secondary"
          onClick={() => onRevoke(session)}
          disabled={isRevoking}
          className="w-full sm:w-auto"
        >
          {isRevoking ? <Trans>Revoking...</Trans> : <Trans>Revoke</Trans>}
        </Button>
      )}
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
  isPending: boolean;
  onRevoke: () => void;
}>) {
  return (
    <AlertDialog open={isOpen} onOpenChange={onOpenChange}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>{t`Revoke session`}</AlertDialogTitle>
          <AlertDialogDescription>
            <Trans>
              Are you sure you want to revoke this session? The device will be signed out and will need to log in again.
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

export default function SessionsModal({ isOpen, onOpenChange }: Readonly<SessionsModalProps>) {
  const [selectedSession, setSelectedSession] = useState<UserSessionInfo | null>(null);
  const [isRevokeDialogOpen, setIsRevokeDialogOpen] = useState(false);
  const [hasRevokedSession, setHasRevokedSession] = useState(false);
  const [revokingSessionIds, setRevokingSessionIds] = useState<Set<string>>(new Set());
  const queryClient = useQueryClient();

  const { data, isLoading } = api.useQuery("get", "/api/account-management/authentication/sessions", {
    enabled: isOpen
  });

  const sessions = data?.sessions ?? [];

  const revokeSessionMutation = api.useMutation("delete", "/api/account-management/authentication/sessions/{id}", {
    onSuccess: () => {
      if (selectedSession) {
        setRevokingSessionIds((prev) => {
          const next = new Set(prev);
          next.delete(selectedSession.id);
          return next;
        });
      }
      toastQueue.add({
        title: t`Success`,
        description: t`Session revoked successfully`,
        variant: "success"
      });
      queryClient.invalidateQueries({ queryKey: ["get", "/api/account-management/authentication/sessions"] });
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

  const handleClose = () => {
    onOpenChange(false);
    setHasRevokedSession(false);
  };

  if (!isOpen) {
    return null;
  }

  return (
    <>
      <Dialog open={isOpen} onOpenChange={onOpenChange}>
        <DialogContent className="sm:w-dialog-xl sm:max-w-none">
          <DialogHeader>
            <DialogTitle>
              <Trans>Sessions</Trans>
            </DialogTitle>
            <DialogDescription>
              <Trans>
                A list of devices that have logged into your account. Revoke any sessions you don't recognize.
              </Trans>
            </DialogDescription>
          </DialogHeader>

          <div className="flex flex-col gap-4">
            {hasRevokedSession && (
              <div className="flex items-center gap-3 rounded-lg border border-border bg-muted/50 p-4">
                <InfoIcon className="h-5 w-5 shrink-0 text-info" />
                <p className="text-sm">
                  <Trans>Please note that it may take up to 5 minutes before the device is signed out.</Trans>
                </p>
              </div>
            )}
            {isLoading ? (
              <div className="flex items-center justify-center py-12">
                <LoaderIcon className="h-8 w-8 animate-spin text-muted-foreground" />
              </div>
            ) : sessions.length === 0 ? (
              <div className="rounded-lg border border-border bg-card p-8 text-center">
                <p className="text-muted-foreground">
                  <Trans>No active sessions found.</Trans>
                </p>
              </div>
            ) : (
              <div className="flex flex-col gap-4">
                {sessions.map((session) => (
                  <SessionCard
                    key={session.id}
                    session={session}
                    isRevoking={revokingSessionIds.has(session.id)}
                    onRevoke={handleRevokeSession}
                  />
                ))}
              </div>
            )}
          </div>
          <DialogFooter>
            <DialogClose render={<Button variant="default" />} onClick={handleClose}>
              <Trans>Close</Trans>
            </DialogClose>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <RevokeSessionDialog
        isOpen={isRevokeDialogOpen}
        onOpenChange={setIsRevokeDialogOpen}
        isPending={revokeSessionMutation.isPending}
        onRevoke={handleConfirmRevoke}
      />
    </>
  );
}
