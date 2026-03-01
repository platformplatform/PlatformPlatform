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
import { TextField } from "@repo/ui/components/TextField";
import { CameraIcon, MailIcon, PencilIcon, Trash2Icon } from "lucide-react";
import { useRef, useState } from "react";
import type { Schemas } from "@/shared/lib/api/client";

const MAX_FILE_SIZE = 1024 * 1024; // 1MB in bytes
const ALLOWED_FILE_TYPES = ["image/jpeg", "image/png", "image/gif", "image/webp"];

export interface UserProfileFieldsProps {
  user: Schemas["CurrentUserResponse"] | undefined;
  isPending: boolean;
  onAvatarFileSelect: (file: File | null) => void;
  onAvatarRemove?: () => void;
  autoFocus?: boolean;
  layout?: "stacked" | "horizontal";
}

export function UserProfileFields({
  user,
  isPending,
  onAvatarFileSelect,
  onAvatarRemove,
  autoFocus,
  layout = "stacked"
}: UserProfileFieldsProps) {
  const [avatarPreviewUrl, setAvatarPreviewUrl] = useState<string | null>(null);
  const [isAvatarRemoved, setIsAvatarRemoved] = useState(false);
  const [avatarMenuOpen, setAvatarMenuOpen] = useState(false);
  const avatarFileInputRef = useRef<HTMLInputElement>(null);

  const onFileSelect = (files: FileList | null) => {
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
      onAvatarFileSelect(file);
    }
  };

  const handleRemove = () => {
    setAvatarMenuOpen(false);
    setAvatarPreviewUrl(null);
    setIsAvatarRemoved(true);
    onAvatarFileSelect(null);
    onAvatarRemove?.();
  };

  const avatarSection = (
    <>
      <input
        type="file"
        ref={avatarFileInputRef}
        onChange={(e) => {
          setAvatarMenuOpen(false);
          onFileSelect(e.target.files);
        }}
        accept={ALLOWED_FILE_TYPES.join(",")}
        className="hidden"
      />

      <DropdownMenu open={avatarMenuOpen} onOpenChange={setAvatarMenuOpen}>
        <div className="group relative">
          <DropdownMenuTrigger
            render={
              <Button
                variant="ghost"
                size="icon"
                className="size-[7rem] rounded-full border border-border border-dashed bg-secondary hover:bg-secondary/80"
                aria-label={t`Change profile picture`}
                disabled={isPending}
              >
                {avatarPreviewUrl || (!isAvatarRemoved && user?.avatarUrl) ? (
                  <img
                    src={avatarPreviewUrl ?? user?.avatarUrl ?? ""}
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
            onClick={() => {
              avatarFileInputRef.current?.click();
            }}
          >
            <CameraIcon className="size-4" />
            <Trans>Upload profile picture</Trans>
          </DropdownMenuItem>
          {(avatarPreviewUrl || (!isAvatarRemoved && user?.avatarUrl)) && (
            <>
              <DropdownMenuSeparator />
              <DropdownMenuItem variant="destructive" onClick={handleRemove}>
                <Trash2Icon className="size-4" />
                <Trans>Remove profile picture</Trans>
              </DropdownMenuItem>
            </>
          )}
        </DropdownMenuContent>
      </DropdownMenu>
    </>
  );

  const fieldsSection = (
    <>
      <div className="flex flex-col gap-4 sm:flex-row">
        <TextField
          autoFocus={autoFocus}
          isRequired={true}
          name="firstName"
          label={t`First name`}
          defaultValue={user?.firstName}
          placeholder={t`E.g. Alex`}
          className="sm:flex-1"
        />
        <TextField
          isRequired={true}
          name="lastName"
          label={t`Last name`}
          defaultValue={user?.lastName}
          placeholder={t`E.g. Taylor`}
          className="sm:flex-1"
        />
      </div>

      <TextField
        name="email"
        label={t`Email`}
        value={user?.email}
        isDisabled={true}
        startIcon={<MailIcon className="size-4" />}
      />

      <TextField
        name="title"
        label={t`Title`}
        tooltip={t`Your professional title or role`}
        defaultValue={user?.title}
        placeholder={t`E.g. Software engineer`}
      />
    </>
  );

  if (layout === "horizontal") {
    return (
      <div className="mt-8 flex flex-col gap-6 md:grid md:grid-cols-[8.5rem_1fr] md:gap-8">
        <div className="flex flex-col">
          <span className="pb-2.75 font-medium text-sm">
            <Trans>Profile photo</Trans>
          </span>
          <div className="flex h-[8.5rem] w-full flex-col items-center justify-center rounded-xl bg-card md:size-[8.5rem]">
            {avatarSection}
          </div>
        </div>
        <div className="flex flex-col gap-4">{fieldsSection}</div>
      </div>
    );
  }

  return (
    <>
      <div className="flex justify-center pb-4">{avatarSection}</div>
      {fieldsSection}
    </>
  );
}
