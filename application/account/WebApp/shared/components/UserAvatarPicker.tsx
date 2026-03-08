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
import { CameraIcon, PencilIcon, Trash2Icon } from "lucide-react";
import { useRef, useState } from "react";

const MAX_FILE_SIZE = 1024 * 1024; // 1MB in bytes
const ALLOWED_FILE_TYPES = ["image/jpeg", "image/png", "image/gif", "image/webp"];

interface UserAvatarPickerProps {
  avatarUrl: string | null | undefined;
  isPending: boolean;
  onFileSelect: (file: File | null) => void;
  onRemove?: () => void;
}

export function UserAvatarPicker({ avatarUrl, isPending, onFileSelect, onRemove }: Readonly<UserAvatarPickerProps>) {
  const [avatarPreviewUrl, setAvatarPreviewUrl] = useState<string | null>(null);
  const [isAvatarRemoved, setIsAvatarRemoved] = useState(false);
  const [avatarMenuOpen, setAvatarMenuOpen] = useState(false);
  const avatarFileInputRef = useRef<HTMLInputElement>(null);

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

      setAvatarPreviewUrl(URL.createObjectURL(file));
      setIsAvatarRemoved(false);
      onFileSelect(file);
    }
  };

  const handleRemove = () => {
    setAvatarMenuOpen(false);
    setAvatarPreviewUrl(null);
    setIsAvatarRemoved(true);
    onFileSelect(null);
    onRemove?.();
  };

  return (
    <>
      <input
        type="file"
        ref={avatarFileInputRef}
        onChange={(e) => {
          setAvatarMenuOpen(false);
          handleFileSelect(e.target.files);
        }}
        accept={ALLOWED_FILE_TYPES.join(",")}
        className="hidden"
      />

      <DropdownMenu open={avatarMenuOpen} onOpenChange={setAvatarMenuOpen} trackingTitle="Profile picture menu">
        <div className="group relative">
          <DropdownMenuTrigger
            render={
              <Button
                variant="ghost"
                size="icon"
                className="size-[7rem] rounded-full border border-dashed border-border bg-secondary hover:bg-secondary/80"
                aria-label={t`Change profile picture`}
                disabled={isPending}
              >
                {avatarPreviewUrl || (!isAvatarRemoved && avatarUrl) ? (
                  <img
                    src={avatarPreviewUrl ?? avatarUrl ?? ""}
                    width={80}
                    height={80}
                    className="size-full rounded-full object-cover"
                    alt={t`Profile avatar`}
                  />
                ) : (
                  <CameraIcon className="size-8 text-secondary-foreground" aria-label={t`Add profile picture`} />
                )}
              </Button>
            }
          />
          <div className="pointer-events-none absolute right-1 bottom-1 flex size-6 items-center justify-center rounded-full border border-border bg-popover opacity-0 group-hover:bg-primary group-hover:opacity-100">
            <PencilIcon className="size-3 text-muted-foreground group-hover:text-primary-foreground" strokeWidth={3} />
          </div>
        </div>
        <DropdownMenuContent>
          <DropdownMenuItem
            trackingLabel="Upload profile picture"
            onClick={() => {
              avatarFileInputRef.current?.click();
            }}
          >
            <CameraIcon className="size-4" />
            <Trans>Upload profile picture</Trans>
          </DropdownMenuItem>
          {(avatarPreviewUrl || (!isAvatarRemoved && avatarUrl)) && (
            <>
              <DropdownMenuSeparator />
              <DropdownMenuItem variant="destructive" trackingLabel="Remove profile picture" onClick={handleRemove}>
                <Trash2Icon className="size-4" />
                <Trans>Remove profile picture</Trans>
              </DropdownMenuItem>
            </>
          )}
        </DropdownMenuContent>
      </DropdownMenu>
    </>
  );
}
