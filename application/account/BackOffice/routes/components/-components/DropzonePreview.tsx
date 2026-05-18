import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { AspectRatio } from "@repo/ui/components/AspectRatio";
import { Button } from "@repo/ui/components/Button";
import { Dropzone } from "@repo/ui/components/Dropzone";
import { XIcon } from "lucide-react";

import { ACCEPTED_IMAGE_TYPES, MAX_FILE_SIZE, type UploadedImage } from "./mediaImages";

interface DropzonePreviewProps {
  images: UploadedImage[];
  onDrop: (files: File[]) => void;
  onRemove: (previewUrl: string) => void;
}

export function DropzonePreview({ images, onDrop, onRemove }: DropzonePreviewProps) {
  return (
    <div className="flex flex-col gap-2">
      <h4>
        <Trans>Dropzone</Trans>
      </h4>
      <p className="text-sm text-muted-foreground">
        <Trans>Drag-and-drop or click to browse. Previews use AspectRatio to keep thumbnails aligned.</Trans>
      </p>
      <Dropzone
        onDrop={onDrop}
        accept={ACCEPTED_IMAGE_TYPES}
        maxSize={MAX_FILE_SIZE}
        multiple
        noDragEventsBubbling
        aria-label={t`Upload images`}
      />
      {images.length > 0 && (
        <div className="mt-2 grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4">
          {images.map((image) => (
            <ImageThumbnail key={image.previewUrl} image={image} onRemove={onRemove} />
          ))}
        </div>
      )}
    </div>
  );
}

interface ImageThumbnailProps {
  image: UploadedImage;
  onRemove: (previewUrl: string) => void;
}

function ImageThumbnail({ image, onRemove }: ImageThumbnailProps) {
  return (
    <div className="flex flex-col gap-1">
      <div className="relative">
        <AspectRatio ratio={1} className="overflow-hidden rounded-md border bg-card">
          <img src={image.previewUrl} alt={image.file.name} className="size-full object-cover" />
        </AspectRatio>
        <Button
          size="icon"
          variant="secondary"
          className="absolute top-1 right-1 size-7"
          onClick={() => onRemove(image.previewUrl)}
          aria-label={t`Remove ${image.file.name}`}
        >
          <XIcon />
        </Button>
      </div>
      <div className="truncate text-xs text-muted-foreground">{image.file.name}</div>
    </div>
  );
}
