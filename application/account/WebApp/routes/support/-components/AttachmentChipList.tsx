import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import {
  ALLOWED_ATTACHMENT_EXTENSIONS,
  formatFileSize,
  MAX_ATTACHMENT_BYTES,
  MAX_ATTACHMENTS
} from "@repo/ui/support/attachments";
import { PaperclipIcon, PlusIcon, XIcon } from "lucide-react";
import { useRef } from "react";
import { toast } from "sonner";

interface AttachmentChipListProps {
  files: File[];
  onFilesChange: (files: File[]) => void;
  disabled?: boolean;
}

export function AttachmentChipList({ files, onFilesChange, disabled }: Readonly<AttachmentChipListProps>) {
  const inputRef = useRef<HTMLInputElement>(null);

  const handleFilesPicked = (picked: FileList | null) => {
    if (!picked) return;
    const accepted: File[] = [];
    const rejected: string[] = [];
    for (const file of Array.from(picked)) {
      const ext = `.${file.name.split(".").pop()?.toLowerCase() ?? ""}`;
      if (!ALLOWED_ATTACHMENT_EXTENSIONS.includes(ext)) {
        rejected.push(t`${file.name} is not an allowed file type`);
        continue;
      }
      if (file.size > MAX_ATTACHMENT_BYTES) {
        rejected.push(t`${file.name} exceeds the 25 MB limit`);
        continue;
      }
      accepted.push(file);
    }
    const combined = [...files, ...accepted].slice(0, MAX_ATTACHMENTS);
    if (files.length + accepted.length > MAX_ATTACHMENTS) {
      rejected.push(t`A ticket can have at most ${MAX_ATTACHMENTS} attachments`);
    }
    for (const message of rejected) {
      toast.error(message);
    }
    onFilesChange(combined);
    if (inputRef.current) inputRef.current.value = "";
  };

  const removeFile = (index: number) => {
    onFilesChange(files.filter((_, i) => i !== index));
  };

  const canAddMore = files.length < MAX_ATTACHMENTS;

  return (
    <div className="flex flex-wrap items-center gap-1.5">
      {files.map((file, index) => (
        <span
          key={`${file.name}-${index}`}
          className="inline-flex items-center gap-1.5 rounded-full border border-border bg-card px-2.5 py-1 text-xs"
        >
          <PaperclipIcon className="size-3 text-muted-foreground" aria-hidden={true} />
          <span className="max-w-[12rem] truncate">{file.name}</span>
          <span className="text-muted-foreground">· {formatFileSize(file.size)}</span>
          <Button
            type="button"
            variant="ghost"
            size="icon"
            className="size-4 rounded-full p-0 hover:bg-muted"
            disabled={disabled}
            onClick={() => removeFile(index)}
            aria-label={t`Remove ${file.name}`}
          >
            <XIcon className="size-3" />
          </Button>
        </span>
      ))}
      {canAddMore && (
        <Button type="button" variant="outline" size="sm" disabled={disabled} onClick={() => inputRef.current?.click()}>
          <PlusIcon className="size-3" />
          <Trans>Add file</Trans>
        </Button>
      )}
      <input
        ref={inputRef}
        type="file"
        multiple={true}
        accept={ALLOWED_ATTACHMENT_EXTENSIONS.join(",")}
        className="hidden"
        onChange={(event) => handleFilesPicked(event.target.files)}
      />
    </div>
  );
}
