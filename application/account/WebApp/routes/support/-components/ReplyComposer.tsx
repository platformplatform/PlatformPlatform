import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { Checkbox } from "@repo/ui/components/Checkbox";
import { Form } from "@repo/ui/components/Form";
import { Label } from "@repo/ui/components/Label";
import { Textarea } from "@repo/ui/components/Textarea";
import { useUnsavedChangesGuard } from "@repo/ui/hooks/useUnsavedChangesGuard";
import {
  ALLOWED_ATTACHMENT_EXTENSIONS,
  MAX_ATTACHMENT_BYTES,
  MAX_ATTACHMENTS,
  formatFileSize
} from "@repo/ui/support/attachments";
import { useMutation } from "@tanstack/react-query";
import { PaperclipIcon, PlusIcon, SendIcon, XIcon } from "lucide-react";
import { useId, useRef, useState } from "react";
import { toast } from "sonner";

import { UnsavedChangesDialog } from "@/shared/components/UnsavedChangesDialog";
import { apiClient, type Schemas } from "@/shared/lib/api/client";

interface ReplyComposerProps {
  ticketId: string;
  onResolved: () => void;
  onSent: () => void;
  // When true, the composer fires /reopen before /reply so the ticket transitions back to
  // AwaitingAgent atomically with the new message. This is the only path that reopens a closed
  // ticket. Clicking the bare "Reopen" button only switches the UI into compose mode.
  reopenBeforeSend?: boolean;
}

export function ReplyComposer({
  ticketId,
  onResolved,
  onSent,
  reopenBeforeSend = false
}: Readonly<ReplyComposerProps>) {
  const [reply, setReply] = useState("");
  const [files, setFiles] = useState<File[]>([]);
  const [markAsResolved, setMarkAsResolved] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);
  const resolveCheckboxId = useId();

  // Dirty when the user has typed or staged any attachments. The shared hook covers TanStack Router
  // navigation, the federated host shell, and the browser tab/window close in one place, and renders
  // the in-app confirm dialog instead of the native browser confirm.
  const isDirty = reply.trim().length > 0 || files.length > 0;
  const { isConfirmDialogOpen, confirmLeave, cancelLeave } = useUnsavedChangesGuard({
    hasUnsavedChanges: isDirty
  });

  const replyMutation = useMutation<void, Schemas["HttpValidationProblemDetails"]>({
    mutationFn: async () => {
      if (reopenBeforeSend) {
        await apiClient.POST("/api/account/support-tickets/{id}/reopen", {
          params: { path: { id: ticketId as Schemas["SupportTicketId"] } }
        });
      }
      const formData = new FormData();
      formData.append("body", reply.trim());
      formData.append("markAsResolved", markAsResolved ? "true" : "false");
      for (const file of files) {
        formData.append("files", file);
      }
      // The openapi-typescript generator misreads `[FromForm]` parameters as `query` params, so
      // we bypass the typed signature and serialize the FormData verbatim. Non-2xx responses are
      // thrown by the shared HTTP middleware as ValidationError/ServerError; TanStack catches
      // those and exposes them on `mutation.error`.
      await apiClient.POST("/api/account/support-tickets/{id}/reply", {
        params: { path: { id: ticketId as Schemas["SupportTicketId"] } },
        body: formData,
        bodySerializer: (value: unknown) => value as FormData
      } as never);
    },
    onSuccess: () => {
      const wasResolved = markAsResolved;
      setReply("");
      setFiles([]);
      setMarkAsResolved(false);
      if (wasResolved) onResolved();
      else onSent();
    }
  });

  const handleFilesPicked = (picked: FileList | null) => {
    if (!picked) return;
    const accepted: File[] = [];
    for (const file of Array.from(picked)) {
      const ext = `.${file.name.split(".").pop()?.toLowerCase() ?? ""}`;
      if (!ALLOWED_ATTACHMENT_EXTENSIONS.includes(ext)) {
        toast.error(t`${file.name} is not an allowed file type`);
        continue;
      }
      if (file.size > MAX_ATTACHMENT_BYTES) {
        toast.error(t`${file.name} exceeds the 25 MB limit`);
        continue;
      }
      accepted.push(file);
    }
    const combined = [...files, ...accepted].slice(0, MAX_ATTACHMENTS);
    if (files.length + accepted.length > MAX_ATTACHMENTS) {
      toast.error(t`A reply can have at most ${MAX_ATTACHMENTS} attachments`);
    }
    setFiles(combined);
    if (inputRef.current) inputRef.current.value = "";
  };

  const removeFile = (index: number) => {
    setFiles((current) => current.filter((_, i) => i !== index));
  };

  const canSend = reply.trim().length > 0 && !replyMutation.isPending;

  const handleSubmit = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (reply.trim().length === 0) return;
    replyMutation.mutate();
  };

  return (
    <div className="border-t border-border bg-background px-4 py-3 sm:px-8 sm:py-4">
      <UnsavedChangesDialog
        isOpen={isConfirmDialogOpen}
        onConfirmLeave={confirmLeave}
        onCancel={cancelLeave}
        parentTrackingTitle="Support ticket detail"
      />
      <Form
        onSubmit={handleSubmit}
        validationErrors={replyMutation.error?.errors}
        validationBehavior="aria"
        className="mx-auto w-full max-w-[48rem]"
      >
        <Textarea
          name="body"
          value={reply}
          onChange={(event) => setReply(event.target.value)}
          placeholder={t`Type your reply…`}
          rows={2}
          maxLength={10000}
          disabled={replyMutation.isPending}
        />
        {files.length > 0 && (
          <div className="mt-2 flex flex-wrap gap-1.5">
            {files.map((file, index) => (
              <AttachmentChip
                key={`${file.name}-${index}`}
                file={file}
                disabled={replyMutation.isPending}
                onRemove={() => removeFile(index)}
              />
            ))}
          </div>
        )}
        <div className="mt-3 flex flex-wrap items-center gap-3">
          <Button
            type="button"
            variant="ghost"
            size="sm"
            disabled={replyMutation.isPending || files.length >= MAX_ATTACHMENTS}
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
            onChange={(event) => handleFilesPicked(event.target.files)}
          />
          <div className="flex-1" />
          <div className="inline-flex items-center gap-2 text-sm text-muted-foreground">
            <Checkbox
              id={resolveCheckboxId}
              checked={markAsResolved}
              onCheckedChange={(value) => setMarkAsResolved(value === true)}
              disabled={replyMutation.isPending}
            />
            <Label htmlFor={resolveCheckboxId} className="cursor-pointer font-normal">
              <Trans>Mark as resolved</Trans>
            </Label>
          </div>
          <Button type="submit" size="sm" disabled={!canSend} isPending={replyMutation.isPending}>
            <SendIcon className="size-3.5" />
            {replyMutation.isPending ? <Trans>Sending...</Trans> : <Trans>Send</Trans>}
          </Button>
        </div>
      </Form>
    </div>
  );
}

function AttachmentChip({ file, disabled, onRemove }: { file: File; disabled: boolean; onRemove: () => void }) {
  return (
    <span className="inline-flex items-center gap-1.5 rounded-full border border-border bg-muted px-2 py-0.5 text-xs">
      <PaperclipIcon className="size-3 text-muted-foreground" aria-hidden={true} />
      <span className="max-w-[10rem] truncate">{file.name}</span>
      <span className="text-muted-foreground">· {formatFileSize(file.size)}</span>
      <Button
        type="button"
        variant="ghost"
        size="icon"
        disabled={disabled}
        className="size-4 rounded-full p-0 hover:bg-background"
        onClick={onRemove}
        aria-label={t`Remove ${file.name}`}
      >
        <XIcon className="size-3" />
      </Button>
    </span>
  );
}
