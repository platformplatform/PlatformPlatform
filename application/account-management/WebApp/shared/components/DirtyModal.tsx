import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import {
  type DirtyModalProps as BaseDirtyModalProps,
  DirtyModal as DirtyModalBase
} from "@repo/ui/components/DirtyModal";

type DirtyModalProps = Omit<
  BaseDirtyModalProps,
  "unsavedChangesTitle" | "unsavedChangesMessage" | "leaveLabel" | "stayLabel"
>;

/**
 * DirtyModal wrapper with translations for account-management.
 * Provides translated unsaved changes dialog text.
 */
export function DirtyModal(props: Readonly<DirtyModalProps>) {
  return (
    <DirtyModalBase
      {...props}
      unsavedChangesTitle={t`Unsaved changes`}
      unsavedChangesMessage={<Trans>You have unsaved changes. If you leave now, your changes will be lost.</Trans>}
      leaveLabel={t`Leave`}
      stayLabel={t`Stay`}
    />
  );
}
