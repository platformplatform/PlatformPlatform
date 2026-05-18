import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { CameraIcon } from "lucide-react";
import { useRef, useState } from "react";

interface AvatarUploadProps {
  onChange?: () => void;
}

export function AvatarUpload({ onChange }: Readonly<AvatarUploadProps>) {
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      setPreviewUrl(URL.createObjectURL(file));
      onChange?.();
    }
  };

  return (
    <div className="flex items-center gap-4">
      <input ref={inputRef} type="file" accept="image/*" className="hidden" onChange={handleFileChange} />
      <button
        type="button"
        onClick={() => inputRef.current?.click()}
        className="flex size-[5rem] shrink-0 items-center justify-center overflow-hidden rounded-full border border-dashed border-border bg-secondary hover:bg-secondary/80 active:bg-secondary/60"
        aria-label={t`Upload profile picture`}
      >
        {previewUrl ? (
          <img src={previewUrl} className="size-full object-cover" alt={t`Profile picture preview`} />
        ) : (
          <CameraIcon className="size-6 text-muted-foreground" />
        )}
      </button>
      <div className="flex flex-col gap-1">
        <Button type="button" variant="outline" size="sm" onClick={() => inputRef.current?.click()}>
          <Trans>Upload photo</Trans>
        </Button>
        {previewUrl && (
          <Button
            type="button"
            variant="ghost"
            size="sm"
            className="text-destructive hover:text-destructive"
            onClick={() => {
              setPreviewUrl(null);
              if (inputRef.current) inputRef.current.value = "";
            }}
          >
            <Trans>Remove</Trans>
          </Button>
        )}
        <p className="text-xs text-muted-foreground">
          <Trans>JPEG, PNG or WebP. Max 1 MB.</Trans>
        </p>
      </div>
    </div>
  );
}
