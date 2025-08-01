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
}

export default function AuthSyncModal({ isOpen, type, newTenantName, onPrimaryAction }: AuthSyncModalProps) {

  const getModalContent = () => {
    switch (type) {
      case "tenant-switch":
        return {
          title: t`Account switched`,
          description: (
            <>
              <Trans>
                Your account was switched to <strong>{newTenantName}</strong> in another browser tab.
              </Trans>
              <div className="mt-2">
                <Trans>
                  Authentication is shared across all tabs. To use multiple accounts simultaneously, please use different browsers.
                </Trans>
              </div>
            </>
          ),
          primaryLabel: <Trans>Continue with {newTenantName}</Trans>
        };

      case "logged-in":
        return {
          title: t`Different user signed in`,
          description: (
            <>
              <Trans>
                A different user signed in from another browser tab.
              </Trans>
              <div className="mt-2">
                <Trans>
                  Authentication is shared across all tabs. To use multiple accounts simultaneously, please use different browsers.
                </Trans>
              </div>
            </>
          ),
          primaryLabel: t`Reload now`
        };

      case "logged-out":
        return {
          title: t`Signed out`,
          description: (
            <>
              <Trans>
                You were signed out from another browser tab.
              </Trans>
              <div className="mt-2">
                <Trans>
                  Authentication is shared across all tabs.
                </Trans>
              </div>
            </>
          ),
          primaryLabel: t`Reload now`
        };

      default:
        // This should never happen due to type constraints
        return {
          title: t`Unknown state`,
          description: t`An unexpected state occurred. Please reload the page.`,
          primaryLabel: t`Reload`
        };
    }
  };

  const content = getModalContent();

  return (
    <Modal isOpen={isOpen} isDismissable={false} isKeyboardDismissDisabled={true}>
      <Dialog className="sm:max-w-lg">
        {() => (
          <>
            <Heading slot="title" className="font-semibold text-lg">
              {content.title}
            </Heading>
            <div className="mt-2 text-muted-foreground text-sm">{content.description}</div>
            <DialogFooter className="mt-6">
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
