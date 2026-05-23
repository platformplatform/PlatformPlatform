import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { Textarea } from "@repo/ui/components/Textarea";
import { ToggleGroup, ToggleGroupItem } from "@repo/ui/components/ToggleGroup";
import { useUnsavedChangesGuard } from "@repo/ui/hooks/useUnsavedChangesGuard";
import { ALLOWED_ATTACHMENT_EXTENSIONS, MAX_ATTACHMENTS } from "@repo/ui/support/attachments";
import { LockIcon, MessageSquareIcon, PlusIcon } from "lucide-react";
import { useRef, useState } from "react";

import { UnsavedChangesDialog } from "@/shared/components/UnsavedChangesDialog";
import { type Schemas, SupportTicketStatus } from "@/shared/lib/api/client";

import { ReopenConfirmDialog } from "./ReopenConfirmDialog";
import { type SendAction, SplitSendButton } from "./SplitSendButton";
import { pickAttachments, StaffAttachmentList } from "./StaffAttachmentList";
import { useStaffReplyMutations } from "./useStaffReplyMutations";

interface StaffReplyComposerProps {
  ticketId: string;
  status: Schemas["SupportTicketStatus"];
}

const TABS = {
  reply: "reply",
  internal: "internal"
} as const;

type ComposerTab = (typeof TABS)[keyof typeof TABS];

export function StaffReplyComposer({ ticketId, status }: Readonly<StaffReplyComposerProps>) {
  // Replying publicly to a Resolved/Closed ticket is rare; the more likely action is to leave an
  // internal note for follow-up. Seed the composer to Internal note so staff don't have to switch.
  const isTerminal = status === SupportTicketStatus.Resolved || status === SupportTicketStatus.Closed;
  const [tab, setTab] = useState<ComposerTab>(isTerminal ? TABS.internal : TABS.reply);
  const [body, setBody] = useState("");
  const [files, setFiles] = useState<File[]>([]);
  const [primaryAction, setPrimaryAction] = useState<SendAction>("send");
  // Holds the pending public-reply action while the reopen confirmation dialog is shown. A public
  // reply on a terminal ticket silently reopens it and emails the user, so staff must confirm.
  const [pendingReopen, setPendingReopen] = useState<SendAction | null>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  const handleSuccess = () => {
    setBody("");
    setFiles([]);
  };

  const { replyMutation, internalNoteMutation, resolveMutation, isAnyPending } = useStaffReplyMutations(
    ticketId,
    handleSuccess
  );

  const hasBody = body.trim().length > 0;
  const hasAttachments = files.length > 0;
  const isDirty = hasBody || hasAttachments;
  const { isConfirmDialogOpen, confirmLeave, cancelLeave } = useUnsavedChangesGuard({
    hasUnsavedChanges: isDirty
  });

  const runPrimary = (action: SendAction) => {
    if (action === "send") replyMutation.mutate({ body: body.trim(), files, markAsResolved: false });
    if (action === "sendAndResolve") replyMutation.mutate({ body: body.trim(), files, markAsResolved: true });
    if (action === "resolve") resolveMutation.mutate();
  };

  const executePrimary = (action: SendAction) => {
    // A public reply on a terminal ticket reopens it and emails the user. Confirm first so staff
    // don't reopen a thread the user considered closed by accident. (SplitSendButton only offers
    // "send" on a terminal ticket, but the guard covers any reply action defensively.)
    if (isTerminal) {
      setPendingReopen(action);
      return;
    }
    runPrimary(action);
  };

  return (
    <div className="border-t border-border bg-background px-4 py-3 sm:px-8 sm:py-4">
      <UnsavedChangesDialog
        isOpen={isConfirmDialogOpen}
        onConfirmLeave={confirmLeave}
        onCancel={cancelLeave}
        parentTrackingTitle="Support ticket detail"
      />
      <div className="mx-auto w-full max-w-[48rem]">
        <ToggleGroup
          variant="outline"
          aria-label={t`Composer mode`}
          value={[tab]}
          onValueChange={(values) => values.length > 0 && setTab(values[values.length - 1] as ComposerTab)}
          className="mb-2"
        >
          <ToggleGroupItem value={TABS.reply}>
            <MessageSquareIcon className="size-3.5" />
            <Trans>Reply to user</Trans>
          </ToggleGroupItem>
          <ToggleGroupItem value={TABS.internal}>
            <LockIcon className="size-3.5" />
            <Trans>Internal note</Trans>
          </ToggleGroupItem>
        </ToggleGroup>
        <Textarea
          value={body}
          onChange={(event) => setBody(event.target.value)}
          placeholder={
            tab === TABS.internal ? t`Internal note (visible only to staff)…` : t`Reply to the user… markdown supported`
          }
          rows={3}
          disabled={isAnyPending}
          className={tab === TABS.internal ? "border-dashed border-warning/40 bg-warning/5" : undefined}
        />
        <StaffAttachmentList
          files={files}
          disabled={isAnyPending}
          onRemove={(index) => setFiles((current) => current.filter((_, i) => i !== index))}
        />
        <div className="mt-3 flex flex-wrap items-center gap-3">
          <Button
            type="button"
            variant="ghost"
            size="sm"
            disabled={isAnyPending || files.length >= MAX_ATTACHMENTS}
            onClick={() => inputRef.current?.click()}
          >
            <PlusIcon className="size-3.5" />
            <Trans>Attach</Trans>
          </Button>
          <input
            ref={inputRef}
            type="file"
            multiple={true}
            accept={ALLOWED_ATTACHMENT_EXTENSIONS.join(",")}
            className="hidden"
            onChange={(event) => {
              setFiles((current) => pickAttachments(event.target.files, current));
              if (inputRef.current) inputRef.current.value = "";
            }}
          />
          <div className="flex-1" />
          {tab === TABS.reply ? (
            <SplitSendButton
              primaryAction={primaryAction}
              hasBody={hasBody}
              hasAttachments={hasAttachments}
              isPending={isAnyPending}
              isTerminal={isTerminal}
              onExecute={executePrimary}
              onSelect={setPrimaryAction}
            />
          ) : (
            <Button
              type="button"
              size="sm"
              disabled={!hasBody}
              isPending={internalNoteMutation.isPending}
              onClick={() => internalNoteMutation.mutate({ body: body.trim(), files })}
            >
              <LockIcon className="size-3.5" />
              {internalNoteMutation.isPending ? <Trans>Saving…</Trans> : <Trans>Save internal note</Trans>}
            </Button>
          )}
        </div>
      </div>

      <ReopenConfirmDialog
        isOpen={pendingReopen !== null}
        onCancel={() => setPendingReopen(null)}
        onConfirm={() => {
          const action = pendingReopen;
          setPendingReopen(null);
          if (action !== null) runPrimary(action);
        }}
      />
    </div>
  );
}
