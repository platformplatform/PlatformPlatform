import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { loggedInPath } from "@repo/infrastructure/auth/constants";
import { Button } from "@repo/ui/components/Button";
import { MailIcon } from "lucide-react";
import { useEffect, useState } from "react";
import { useSwitchTenant } from "@/shared/hooks/useSwitchTenant";
import type { components } from "@/shared/lib/api/api.generated";
import { api } from "@/shared/lib/api/client";
import { AcceptInvitationDialog } from "../common/AcceptInvitationDialog";
import { SwitchingAccountLoader } from "../common/SwitchingAccountLoader";

type TenantInfo = components["schemas"]["TenantInfo"];

const BANNER_HEIGHT = "3rem";

export default function InvitationBanner() {
  const [invitationDialogTenant, setInvitationDialogTenant] = useState<TenantInfo | null>(null);
  const [isSwitching, setIsSwitching] = useState(false);

  const { data: tenantsResponse } = api.useQuery("get", "/api/account/tenants");

  const pendingInvitations = (tenantsResponse?.tenants ?? []).filter((tenant) => tenant.isNew);
  const hasPendingInvitations = pendingInvitations.length > 0;

  const { switchTenant } = useSwitchTenant({
    onMutate: () => {
      setIsSwitching(true);
    },
    onSuccess: () => {
      const targetPath = window.location.pathname === "/" ? loggedInPath : window.location.pathname;
      window.location.href = targetPath;
    },
    onError: () => {
      setIsSwitching(false);
      setInvitationDialogTenant(null);
    }
  });

  const handleAcceptInvitation = () => {
    if (invitationDialogTenant) {
      switchTenant(invitationDialogTenant);
    }
  };

  const handleViewInvitation = () => {
    if (pendingInvitations.length > 0) {
      setInvitationDialogTenant(pendingInvitations[0]);
    }
  };

  useEffect(() => {
    if (hasPendingInvitations) {
      document.documentElement.style.setProperty("--invitation-banner-height", BANNER_HEIGHT);
    } else {
      document.documentElement.style.setProperty("--invitation-banner-height", "0rem");
    }
    return () => {
      document.documentElement.style.setProperty("--invitation-banner-height", "0rem");
    };
  }, [hasPendingInvitations]);

  if (!hasPendingInvitations) {
    return null;
  }

  const firstInvitation = pendingInvitations[0];
  const invitationCount = pendingInvitations.length;

  return (
    <>
      <div className="fixed top-0 right-0 left-0 z-[48] flex h-12 items-center gap-3 border-warning/50 border-b bg-warning px-4 text-sm">
        <MailIcon className="size-4 shrink-0 text-warning-foreground" />
        <span className="flex-1 text-warning-foreground">
          {invitationCount === 1 ? (
            <Trans>
              You have been invited to join <strong>{firstInvitation.tenantName}</strong>.
            </Trans>
          ) : (
            <Trans>You have {invitationCount} pending invitations.</Trans>
          )}
        </span>
        <Button variant="default" size="sm" onClick={handleViewInvitation} disabled={isSwitching}>
          {isSwitching ? t`Loading...` : t`View invitation`}
        </Button>
      </div>

      <AcceptInvitationDialog
        isOpen={!!invitationDialogTenant}
        onOpenChange={(open) => !open && setInvitationDialogTenant(null)}
        tenant={invitationDialogTenant}
        onAccept={handleAcceptInvitation}
        isLoading={isSwitching}
      />
      {isSwitching && <SwitchingAccountLoader />}
    </>
  );
}
