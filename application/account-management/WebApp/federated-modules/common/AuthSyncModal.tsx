import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { Dialog } from "@repo/ui/components/Dialog";
import { DialogFooter } from "@repo/ui/components/DialogFooter";
import { Heading } from "@repo/ui/components/Heading";
import { Modal } from "@repo/ui/components/Modal";
import { useEffect, useState } from "react";

export type AuthSyncModalType = "tenant-switch" | "logged-in" | "logged-out";

export interface AuthSyncModalProps {
  isOpen: boolean;
  type: AuthSyncModalType;
  currentTenantName?: string;
  newTenantName?: string;
  onPrimaryAction: () => void;
  onSecondaryAction?: () => void;
}

export default function AuthSyncModal({
  isOpen,
  type,
  currentTenantName,
  newTenantName,
  onPrimaryAction,
  onSecondaryAction
}: AuthSyncModalProps) {
  const [countdown, setCountdown] = useState(3);

  // Handle countdown for auto-reload scenarios
  useEffect(() => {
    if (!isOpen || type === "tenant-switch") {
      setCountdown(3);
      return;
    }

    const timer = setInterval(() => {
      setCountdown((prev) => {
        if (prev <= 1) {
          clearInterval(timer);
          onPrimaryAction();
          return 0;
        }
        return prev - 1;
      });
    }, 1000);

    return () => clearInterval(timer);
  }, [isOpen, type, onPrimaryAction]);

  const getModalContent = () => {
    switch (type) {
      case "tenant-switch":
        return {
          title: t`Account switched`,
          description: (
            <Trans>
              Your account has been switched to <strong>{newTenantName}</strong> in another tab.
            </Trans>
          ),
          primaryLabel: <Trans>Continue with {newTenantName}</Trans>,
          secondaryLabel: currentTenantName ? <Trans>Switch back to {currentTenantName}</Trans> : undefined
        };

      case "logged-in":
        return {
          title: t`Logged in`,
          description: t`You've been logged in from another tab. This page will reload in ${countdown} seconds.`,
          primaryLabel: t`Reload now`,
          secondaryLabel: undefined
        };

      case "logged-out":
        return {
          title: t`Logged out`,
          description: t`You've been logged out from another tab. This page will reload in ${countdown} seconds.`,
          primaryLabel: t`Reload now`,
          secondaryLabel: undefined
        };

      default:
        // This should never happen due to type constraints
        return {
          title: t`Unknown state`,
          description: t`An unexpected state occurred. Please reload the page.`,
          primaryLabel: t`Reload`,
          secondaryLabel: undefined
        };
    }
  };

  const content = getModalContent();

  return (
    <Modal isOpen={isOpen} isDismissable={false} isKeyboardDismissDisabled={true}>
      <Dialog>
        {() => (
          <>
            <Heading slot="title" className="font-semibold text-lg">
              {content.title}
            </Heading>
            <div className="mt-2 text-muted-foreground text-sm">{content.description}</div>
            <DialogFooter className="mt-6">
              {content.secondaryLabel && onSecondaryAction && (
                <Button variant="secondary" onPress={onSecondaryAction}>
                  {content.secondaryLabel}
                </Button>
              )}
              <Button variant="primary" onPress={onPrimaryAction}>
                {content.primaryLabel}
              </Button>
            </DialogFooter>
          </>
        )}
      </Dialog>
    </Modal>
  );
}
