import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { AlertDialog } from "@repo/ui/components/AlertDialog";
import { Badge } from "@repo/ui/components/Badge";
import { Button } from "@repo/ui/components/Button";
import { Dialog } from "@repo/ui/components/Dialog";
import { DialogContent, DialogFooter, DialogHeader } from "@repo/ui/components/DialogFooter";
import { Heading } from "@repo/ui/components/Heading";
import { Modal } from "@repo/ui/components/Modal";
import { toastQueue } from "@repo/ui/components/Toast";
import { formatDate } from "@repo/utils/date/formatDate";
import { useQueryClient } from "@tanstack/react-query";
import { InfoIcon, LaptopIcon, LoaderIcon, MonitorIcon, SmartphoneIcon, TabletIcon, XIcon } from "lucide-react";
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
  onRevoke
}: Readonly<{ session: UserSessionInfo; onRevoke: (session: UserSessionInfo) => void }>) {
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
              <Badge variant="success">
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
        <Button variant="secondary" onPress={() => onRevoke(session)} className="w-full sm:w-auto">
          <Trans>Revoke</Trans>
        </Button>
      )}
    </div>
  );
}

function RevokeSessionDialog({
  isOpen,
  onOpenChange,
  isPending,
  onRevoke
}: Readonly<{
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
  isPending: boolean;
  onRevoke: () => void;
}>) {
  return (
    <Modal isOpen={isOpen} onOpenChange={onOpenChange} blur={false} isDismissable={!isPending} zIndex="high">
      <AlertDialog
        title={t`Revoke session`}
        variant="destructive"
        actionLabel={t`Revoke`}
        cancelLabel={t`Cancel`}
        onAction={onRevoke}
      >
        <Trans>
          Are you sure you want to revoke this session? The device will be signed out and will need to log in again.
        </Trans>
      </AlertDialog>
    </Modal>
  );
}

export default function SessionsModal({ isOpen, onOpenChange }: Readonly<SessionsModalProps>) {
  const [selectedSession, setSelectedSession] = useState<UserSessionInfo | null>(null);
  const [isRevokeDialogOpen, setIsRevokeDialogOpen] = useState(false);
  const [hasRevokedSession, setHasRevokedSession] = useState(false);
  const queryClient = useQueryClient();

  const { data, isLoading } = api.useQuery("get", "/api/account-management/authentication/sessions", {
    enabled: isOpen
  });

  const sessions = data?.sessions ?? [];

  const revokeSessionMutation = api.useMutation("delete", "/api/account-management/authentication/sessions/{id}", {
    onSuccess: () => {
      toastQueue.add({
        title: t`Success`,
        description: t`Session revoked successfully`,
        variant: "success"
      });
      queryClient.invalidateQueries({ queryKey: ["get", "/api/account-management/authentication/sessions"] });
      setIsRevokeDialogOpen(false);
      setSelectedSession(null);
      setHasRevokedSession(true);
    }
  });

  const handleRevokeSession = (session: UserSessionInfo) => {
    setSelectedSession(session);
    setIsRevokeDialogOpen(true);
  };

  const handleConfirmRevoke = () => {
    if (selectedSession) {
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
      <Modal isOpen={isOpen} onOpenChange={onOpenChange} isDismissable={true}>
        <Dialog aria-label={t`Sessions`} className="max-sm:flex max-sm:flex-col max-sm:overflow-hidden sm:w-dialog-xl">
          <XIcon onClick={handleClose} className="absolute top-2 right-2 h-10 w-10 cursor-pointer p-2 hover:bg-muted" />
          <DialogHeader
            description={
              <Trans>
                A list of devices that have logged into your account. Revoke any sessions you don't recognize.
              </Trans>
            }
          >
            <Heading slot="title" className="text-2xl">
              <Trans>Sessions</Trans>
            </Heading>
          </DialogHeader>

          <DialogContent className="flex flex-col gap-4">
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
              <div className="flex max-h-96 flex-col gap-4 overflow-y-auto">
                {sessions.map((session) => (
                  <SessionCard key={session.id} session={session} onRevoke={handleRevokeSession} />
                ))}
              </div>
            )}
          </DialogContent>
          <DialogFooter>
            <Button onPress={handleClose}>
              <Trans>Close</Trans>
            </Button>
          </DialogFooter>
        </Dialog>
      </Modal>

      <RevokeSessionDialog
        isOpen={isRevokeDialogOpen}
        onOpenChange={setIsRevokeDialogOpen}
        isPending={revokeSessionMutation.isPending}
        onRevoke={handleConfirmRevoke}
      />
    </>
  );
}
