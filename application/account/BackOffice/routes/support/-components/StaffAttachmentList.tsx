import { t } from "@lingui/core/macro";
import { Button } from "@repo/ui/components/Button";
import { PaperclipIcon, XIcon } from "lucide-react";
import { toast } from "sonner";

import { ALLOWED_ATTACHMENT_EXTENSIONS, formatFileSize, MAX_ATTACHMENT_BYTES, MAX_ATTACHMENTS } from "./formatFileSize";

export function pickAttachments(picked: FileList | null, current: File[]): File[] {
  if (!picked) return current;
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
  if (current.length + accepted.length > MAX_ATTACHMENTS) {
    toast.error(t`A reply can have at most ${MAX_ATTACHMENTS} attachments`);
  }
  return [...current, ...accepted].slice(0, MAX_ATTACHMENTS);
}

interface StaffAttachmentListProps {
  files: File[];
  disabled: boolean;
  onRemove: (index: number) => void;
}

export function StaffAttachmentList({ files, disabled, onRemove }: Readonly<StaffAttachmentListProps>) {
  if (files.length === 0) return null;
  return (
    <div className="mt-2 flex flex-wrap gap-1.5">
      {files.map((file, index) => (
        <span
          key={`${file.name}-${index}`}
          className="inline-flex items-center gap-1.5 rounded-full border border-border bg-muted px-2 py-0.5 text-xs"
        >
          <PaperclipIcon className="size-3 text-muted-foreground" aria-hidden={true} />
          <span className="max-w-[10rem] truncate">{file.name}</span>
          <span className="text-muted-foreground">· {formatFileSize(file.size)}</span>
          <Button
            type="button"
            variant="ghost"
            size="icon"
            disabled={disabled}
            className="size-4 rounded-full p-0 hover:bg-background"
            onClick={() => onRemove(index)}
            aria-label={t`Remove ${file.name}`}
          >
            <XIcon className="size-3" />
          </Button>
        </span>
      ))}
    </div>
  );
}
