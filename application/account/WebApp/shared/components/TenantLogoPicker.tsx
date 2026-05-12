import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger
} from "@repo/ui/components/DropdownMenu";
import { type Accept, type FileRejection, useDropzone } from "@repo/ui/components/Dropzone";
import { TenantLogo } from "@repo/ui/components/TenantLogo";
import { CameraIcon, PencilIcon, Trash2Icon } from "lucide-react";
import { useRef, useState } from "react";

const MAX_FILE_SIZE = 1024 * 1024; // 1MB in bytes
const ALLOWED_FILE_TYPES = ["image/jpeg", "image/png", "image/gif", "image/webp"];
const ACCEPTED_FILES: Accept = {
  "image/jpeg": [".jpg", ".jpeg"],
  "image/png": [".png"],
  "image/gif": [".gif"],
  "image/webp": [".webp"]
};

interface TenantLogoPickerProps {
  logoUrl: string | null | undefined;
  tenantName: string;
  isPending: boolean;
  readOnly?: boolean;
  size: "base" | "lg";
  onFileSelect: (file: File | null) => void;
  onRemove?: () => void;
}

export function TenantLogoPicker({
  logoUrl,
  tenantName,
  isPending,
  readOnly,
  size,
  onFileSelect,
  onRemove
}: Readonly<TenantLogoPickerProps>) {
  const [logoPreviewUrl, setLogoPreviewUrl] = useState<string | null>(null);
  const [isLogoRemoved, setIsLogoRemoved] = useState(false);
  const [logoMenuOpen, setLogoMenuOpen] = useState(false);
  const logoFileInputRef = useRef<HTMLInputElement>(null);

  const handleAcceptedFile = (file: File) => {
    setLogoPreviewUrl(URL.createObjectURL(file));
    setIsLogoRemoved(false);
    onFileSelect(file);
  };

  const handleRejection = (rejection: FileRejection) => {
    const code = rejection.errors[0]?.code;
    if (code === "file-too-large") {
      alert(t`Image must be smaller than 1 MB.`);
    } else if (code === "file-invalid-type") {
      alert(t`Please select a JPEG, PNG, GIF, or WebP image.`);
    }
  };

  const handleFileSelect = (files: FileList | null) => {
    if (files?.[0]) {
      const file = files[0];

      if (!ALLOWED_FILE_TYPES.includes(file.type)) {
        alert(t`Please select a JPEG, PNG, GIF, or WebP image.`);
        return;
      }

      if (file.size > MAX_FILE_SIZE) {
        alert(t`Image must be smaller than 1 MB.`);
        return;
      }

      handleAcceptedFile(file);
    }
  };

  const { getRootProps, isDragActive } = useDropzone({
    onDrop: (acceptedFiles, fileRejections) => {
      if (fileRejections[0]) {
        handleRejection(fileRejections[0]);
        return;
      }
      if (acceptedFiles[0]) {
        handleAcceptedFile(acceptedFiles[0]);
      }
    },
    accept: ACCEPTED_FILES,
    maxSize: MAX_FILE_SIZE,
    multiple: false,
    noClick: true,
    noKeyboard: true,
    disabled: readOnly || isPending
  });

  const handleRemove = () => {
    setLogoMenuOpen(false);
    setLogoPreviewUrl(null);
    setIsLogoRemoved(true);
    onFileSelect(null);
    onRemove?.();
  };

  const buttonSizeClass = size === "lg" ? "size-[7rem]" : "size-16";

  return (
    <>
      <input
        type="file"
        ref={logoFileInputRef}
        onChange={(e) => {
          setLogoMenuOpen(false);
          handleFileSelect(e.target.files);
        }}
        accept={ALLOWED_FILE_TYPES.join(",")}
        className="hidden"
      />

      <DropdownMenu open={logoMenuOpen} onOpenChange={setLogoMenuOpen} trackingTitle="Account logo menu">
        <div
          {...getRootProps({
            className: [
              "relative",
              size === "lg" &&
                "flex h-[8.5rem] w-full flex-col items-center justify-center rounded-xl bg-card md:size-[8.5rem]",
              !readOnly && "group",
              isDragActive && "rounded-xl outline outline-2 outline-dashed outline-ring"
            ]
              .filter(Boolean)
              .join(" ")
          })}
        >
          <DropdownMenuTrigger
            disabled={readOnly}
            render={
              <Button
                variant="ghost"
                size="icon"
                className={`rounded-md ${buttonSizeClass}`}
                aria-label={t`Change logo`}
                disabled={readOnly || isPending}
              >
                <TenantLogo
                  key={logoPreviewUrl ?? (isLogoRemoved ? "no-logo" : (logoUrl ?? "no-logo"))}
                  logoUrl={logoPreviewUrl ?? (isLogoRemoved ? undefined : logoUrl)}
                  tenantName={tenantName}
                  size="lg"
                  className={size === "lg" ? "size-[7rem]" : undefined}
                />
              </Button>
            }
          />
          {!readOnly && (
            <div className="pointer-events-none absolute right-0 bottom-0 flex size-6 items-center justify-center rounded-full border border-border bg-popover opacity-0 group-hover:bg-primary group-hover:opacity-100">
              <PencilIcon
                className="size-3 text-muted-foreground group-hover:text-primary-foreground"
                strokeWidth={3}
              />
            </div>
          )}
        </div>
        <DropdownMenuContent>
          <DropdownMenuItem
            trackingLabel="Upload logo"
            onClick={() => {
              logoFileInputRef.current?.click();
            }}
          >
            <CameraIcon className="size-4" />
            <Trans>Upload logo</Trans>
          </DropdownMenuItem>
          {(logoPreviewUrl || (!isLogoRemoved && logoUrl)) && (
            <>
              <DropdownMenuSeparator />
              <DropdownMenuItem variant="destructive" trackingLabel="Remove logo" onClick={handleRemove}>
                <Trash2Icon className="size-4" />
                <Trans>Remove logo</Trans>
              </DropdownMenuItem>
            </>
          )}
        </DropdownMenuContent>
      </DropdownMenu>
    </>
  );
}
