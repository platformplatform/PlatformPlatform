import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { AspectRatio } from "@repo/ui/components/AspectRatio";
import { Button } from "@repo/ui/components/Button";
import { Dropzone } from "@repo/ui/components/Dropzone";
import { ImageIcon, XIcon } from "lucide-react";
import { useCallback, useEffect, useState } from "react";

import { ACCEPTED_IMAGE_TYPES, MAX_FILE_SIZE } from "./mediaImages";

export function AspectRatioPreview() {
  return (
    <div className="flex flex-col gap-2">
      <h4>
        <Trans>Aspect ratio</Trans>
      </h4>
      <p className="text-sm text-muted-foreground">
        <Trans>Drop an image into any card — AspectRatio keeps the slot size fixed regardless of content.</Trans>
      </p>
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <RatioCard ratio={1} label="1 / 1 (square)" />
        <RatioCard ratio={4 / 3} label="4 / 3" />
        <RatioCard ratio={16 / 9} label="16 / 9" />
        <RatioCard ratio={3 / 4} label="3 / 4 (portrait)" />
      </div>
    </div>
  );
}

interface RatioCardProps {
  ratio: number;
  label: string;
}

function RatioCard({ ratio, label }: RatioCardProps) {
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);

  useEffect(() => {
    return () => {
      if (previewUrl) {
        URL.revokeObjectURL(previewUrl);
      }
    };
  }, [previewUrl]);

  const handleDrop = useCallback((acceptedFiles: File[]) => {
    const file = acceptedFiles[0];
    if (!file) return;
    setPreviewUrl((previous) => {
      if (previous) URL.revokeObjectURL(previous);
      return URL.createObjectURL(file);
    });
  }, []);

  const handleRemove = () => {
    setPreviewUrl((previous) => {
      if (previous) URL.revokeObjectURL(previous);
      return null;
    });
  };

  return (
    <div className="flex flex-col gap-2">
      <AspectRatio ratio={ratio} className="overflow-hidden rounded-md">
        <Dropzone
          onDrop={handleDrop}
          accept={ACCEPTED_IMAGE_TYPES}
          maxSize={MAX_FILE_SIZE}
          noDragEventsBubbling
          aria-label={t`Upload image for ${label}`}
          className="h-full p-2"
        >
          {previewUrl ? (
            <img src={previewUrl} alt={label} className="size-full rounded-sm object-cover" />
          ) : (
            <ImageIcon className="size-10 text-muted-foreground" />
          )}
        </Dropzone>
        {previewUrl && (
          <Button
            size="icon"
            variant="secondary"
            className="absolute top-1 right-1 size-7"
            onClick={handleRemove}
            aria-label={t`Remove image from ${label}`}
          >
            <XIcon />
          </Button>
        )}
      </AspectRatio>
      <div className="text-center text-xs text-muted-foreground">{label}</div>
    </div>
  );
}
