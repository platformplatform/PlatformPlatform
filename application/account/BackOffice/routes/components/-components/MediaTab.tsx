import { Trans } from "@lingui/react/macro";
import { UploadIcon } from "lucide-react";
import { useCallback, useEffect, useState } from "react";

import { AspectRatioPreview } from "./AspectRatioPreview";
import { DropzonePreview } from "./DropzonePreview";
import { ACCEPTED_IMAGE_TYPES, type UploadedImage } from "./mediaImages";

const ACCEPTED_MIME_TYPES = new Set(Object.keys(ACCEPTED_IMAGE_TYPES));

export function MediaTab() {
  const [images, setImages] = useState<UploadedImage[]>([]);
  const [isDragging, setIsDragging] = useState(false);

  useEffect(() => {
    return () => {
      for (const image of images) {
        URL.revokeObjectURL(image.previewUrl);
      }
    };
  }, [images]);

  const addFilesToGallery = useCallback((files: File[]) => {
    const newImages = files.map((file) => ({
      file,
      previewUrl: URL.createObjectURL(file)
    }));
    setImages((previous) => [...previous, ...newImages]);
  }, []);

  const handleRemove = (previewUrl: string) => {
    setImages((previous) => {
      const image = previous.find((item) => item.previewUrl === previewUrl);
      if (image) {
        URL.revokeObjectURL(image.previewUrl);
      }
      return previous.filter((item) => item.previewUrl !== previewUrl);
    });
  };

  useEffect(() => {
    let dragCounter = 0;

    const isFilesDrag = (event: DragEvent) => event.dataTransfer?.types?.includes("Files") ?? false;

    const handleDragEnter = (event: DragEvent) => {
      if (!isFilesDrag(event)) return;
      dragCounter++;
      if (dragCounter === 1) setIsDragging(true);
    };

    const handleDragLeave = (event: DragEvent) => {
      if (!isFilesDrag(event)) return;
      dragCounter--;
      if (dragCounter <= 0) {
        dragCounter = 0;
        setIsDragging(false);
      }
    };

    const handleDragOver = (event: DragEvent) => {
      if (!isFilesDrag(event)) return;
      event.preventDefault();
    };

    const handleDropReset = () => {
      dragCounter = 0;
      setIsDragging(false);
    };

    const handleWindowDrop = (event: DragEvent) => {
      if (!isFilesDrag(event)) return;
      event.preventDefault();
      const files = Array.from(event.dataTransfer?.files ?? []);
      const imageFiles = files.filter((file) => ACCEPTED_MIME_TYPES.has(file.type));
      if (imageFiles.length > 0) {
        addFilesToGallery(imageFiles);
      }
    };

    window.addEventListener("dragenter", handleDragEnter, true);
    window.addEventListener("dragleave", handleDragLeave, true);
    window.addEventListener("dragover", handleDragOver, true);
    window.addEventListener("drop", handleDropReset, true);
    window.addEventListener("drop", handleWindowDrop, false);

    return () => {
      window.removeEventListener("dragenter", handleDragEnter, true);
      window.removeEventListener("dragleave", handleDragLeave, true);
      window.removeEventListener("dragover", handleDragOver, true);
      window.removeEventListener("drop", handleDropReset, true);
      window.removeEventListener("drop", handleWindowDrop, false);
    };
  }, [addFilesToGallery]);

  return (
    <>
      {isDragging && (
        <div className="pointer-events-none fixed inset-px z-50 flex items-center justify-center rounded-xl outline outline-2 -outline-offset-2 outline-ring outline-dashed">
          <div className="flex flex-col items-center gap-2 text-primary">
            <UploadIcon className="size-12" />
            <div className="font-medium">
              <Trans>Drop anywhere to add to the gallery</Trans>
            </div>
          </div>
        </div>
      )}
      <div className="flex flex-col gap-12">
        <DropzonePreview images={images} onDrop={addFilesToGallery} onRemove={handleRemove} />
        <AspectRatioPreview />
      </div>
    </>
  );
}
