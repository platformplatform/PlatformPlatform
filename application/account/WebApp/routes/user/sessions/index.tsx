import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { useQueryClient } from "@tanstack/react-query";
import { createFileRoute } from "@tanstack/react-router";
import { InfoIcon, LoaderIcon } from "lucide-react";
import { useState } from "react";
import { toast } from "sonner";

import { api } from "@/shared/lib/api/client";

import type { UserSessionInfo } from "./-components/sessionUtils";

import { RevokeSessionDialog } from "./-components/RevokeSessionDialog";
import { SessionCard } from "./-components/SessionCard";

export const Route = createFileRoute("/user/sessions/")({
  staticData: { trackingTitle: "User sessions" },
  component: SessionsPage
});

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
