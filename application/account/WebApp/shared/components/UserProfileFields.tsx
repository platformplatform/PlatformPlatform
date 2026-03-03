import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { TextField } from "@repo/ui/components/TextField";
import { MailIcon } from "lucide-react";

import { UserAvatarPicker } from "./UserAvatarPicker";

interface UserData {
  avatarUrl: string | null;
  firstName: string | null;
  lastName: string | null;
  email: string;
  title: string | null;
}

export interface UserProfileFieldsProps {
  user: UserData | undefined;
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
  const avatarSection = (
    <UserAvatarPicker
      avatarUrl={user?.avatarUrl}
      isPending={isPending}
      size={layout === "horizontal" ? "lg" : "base"}
      onFileSelect={onAvatarFileSelect}
      onRemove={onAvatarRemove}
    />
  );

  const fieldsSection = (
    <>
      <div className="flex flex-col gap-4 sm:flex-row">
        <TextField
          autoFocus={autoFocus}
          required={true}
          name="firstName"
          label={t`First name`}
          defaultValue={user?.firstName ?? undefined}
          placeholder={t`E.g. Alex`}
          className="sm:flex-1"
        />
        <TextField
          required={true}
          name="lastName"
          label={t`Last name`}
          defaultValue={user?.lastName ?? undefined}
          placeholder={t`E.g. Taylor`}
          className="sm:flex-1"
        />
      </div>

      <TextField
        name="email"
        label={t`Email`}
        tooltip={t`Your email address cannot be changed. An owner must delete your account and reinvite you with the new email address.`}
        value={user?.email}
        readOnly={true}
        startIcon={<MailIcon className="size-4" />}
      />

      <TextField
        name="title"
        label={t`Title`}
        tooltip={t`Your professional title or role`}
        defaultValue={user?.title ?? undefined}
        placeholder={t`E.g. Software engineer`}
      />
    </>
  );

  if (layout === "horizontal") {
    return (
      <div className="mt-8 flex flex-col gap-6 md:grid md:grid-cols-[8.5rem_1fr] md:gap-8">
        <div className="flex flex-col">
          <span className="pb-2.75 text-sm font-medium">
            <Trans>Profile photo</Trans>
          </span>
          {avatarSection}
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
