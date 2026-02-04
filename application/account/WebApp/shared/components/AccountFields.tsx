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
import { TenantLogo } from "@repo/ui/components/TenantLogo";
import { TextField } from "@repo/ui/components/TextField";
import { CameraIcon, Trash2Icon } from "lucide-react";
import { useRef, useState } from "react";
import type { Schemas } from "@/shared/lib/api/client";

const MAX_FILE_SIZE = 1024 * 1024; // 1MB in bytes
const ALLOWED_FILE_TYPES = ["image/jpeg", "image/png", "image/gif", "image/webp", "image/svg+xml"];

export interface AccountFieldsProps {
  tenant: Schemas["TenantResponse"] | undefined;
  isPending: boolean;
  onLogoFileSelect: (file: File | null) => void;
  onLogoRemove?: () => void;
  isReadOnly?: boolean;
  tooltip?: string;
  description?: string;
  onChange?: () => void;
}

export function AccountFields({
  tenant,
  isPending,
  onLogoFileSelect,
  onLogoRemove,
  isReadOnly,
  tooltip,
  description,
  onChange
}: AccountFieldsProps) {
  const [logoPreviewUrl, setLogoPreviewUrl] = useState<string | null>(null);
  const [logoMenuOpen, setLogoMenuOpen] = useState(false);
  const logoFileInputRef = useRef<HTMLInputElement>(null);

  const onFileSelect = (files: FileList | null) => {
    if (files?.[0]) {
      const file = files[0];

      if (!ALLOWED_FILE_TYPES.includes(file.type)) {
        alert(t`Please select a JPEG, PNG, GIF, WebP, or SVG image.`);
        return;
      }

      if (file.size > MAX_FILE_SIZE) {
        alert(t`Image must be smaller than 1 MB.`);
        return;
      }

      setLogoPreviewUrl(URL.createObjectURL(file));
      onLogoFileSelect(file);
    }
  };

  const handleRemove = () => {
    setLogoMenuOpen(false);
    setLogoPreviewUrl(null);
    onLogoFileSelect(null);
    onLogoRemove?.();
  };

  return (
    <>
      <input
        type="file"
        ref={logoFileInputRef}
        onChange={(e) => {
          setLogoMenuOpen(false);
          onFileSelect(e.target.files);
        }}
        accept={ALLOWED_FILE_TYPES.join(",")}
        className="hidden"
      />

      <div className="flex justify-center pb-4">
        <DropdownMenu open={logoMenuOpen} onOpenChange={setLogoMenuOpen}>
          <DropdownMenuTrigger
            disabled={isReadOnly}
            render={
              <Button
                variant="ghost"
                size="icon"
                className="size-16 rounded-md"
                aria-label={t`Change logo`}
                disabled={isReadOnly || isPending}
              >
                <TenantLogo
                  key={logoPreviewUrl ?? tenant?.logoUrl ?? "no-logo"}
                  logoUrl={logoPreviewUrl ?? tenant?.logoUrl}
                  tenantName={tenant?.name ?? ""}
                  size="lg"
                />
              </Button>
            }
          />
          <DropdownMenuContent>
            <DropdownMenuItem
              onClick={() => {
                logoFileInputRef.current?.click();
              }}
            >
              <CameraIcon className="size-4" />
              <Trans>Upload logo</Trans>
            </DropdownMenuItem>
            {(tenant?.logoUrl || logoPreviewUrl) && (
              <>
                <DropdownMenuSeparator />
                <DropdownMenuItem variant="destructive" onClick={handleRemove}>
                  <Trash2Icon className="size-4" />
                  <Trans>Remove logo</Trans>
                </DropdownMenuItem>
              </>
            )}
          </DropdownMenuContent>
        </DropdownMenu>
      </div>

      <TextField
        isRequired={true}
        name="name"
        defaultValue={tenant?.name ?? ""}
        isDisabled={isPending}
        isReadOnly={isReadOnly}
        label={t`Account name`}
        placeholder={t`E.g. Acme Corp`}
        tooltip={tooltip}
        description={description}
        onChange={onChange}
      />
    </>
  );
}
