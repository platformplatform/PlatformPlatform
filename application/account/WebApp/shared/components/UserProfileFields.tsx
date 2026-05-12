import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { TextField } from "@repo/ui/components/TextField";
import { MailIcon } from "lucide-react";
import { type ReactNode, useState } from "react";

import type { Schemas } from "@/shared/lib/api/client";

import { UserAvatarPicker } from "./UserAvatarPicker";

export interface UserProfileFieldsProps {
  user: Schemas["CurrentUserResponse"] | undefined;
  isPending: boolean;
  onAvatarFileSelect: (file: File | null) => void;
  onAvatarRemove?: () => void;
  autoFocus?: boolean;
  layout?: "stacked" | "horizontal";
  infoFields?: ReactNode;
}

export function UserProfileFields({
  user,
  isPending,
  onAvatarFileSelect,
  onAvatarRemove,
  autoFocus,
  layout = "stacked",
  infoFields
}: UserProfileFieldsProps) {
  // Snapshot user once so TextField defaultValue stays stable. A later refetch (e.g. after
  // saving the profile) would otherwise change defaultValue between renders and trigger
  // Base UI's "default value of an uncontrolled FieldControl after being initialized" warning.
  const [initialUser, setInitialUser] = useState(user);
  if (initialUser === undefined && user !== undefined) {
    setInitialUser(user);
  }

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
          defaultValue={initialUser?.firstName ?? ""}
          placeholder={t`E.g. Alex`}
          className="sm:flex-1"
        />
        <TextField
          required={true}
          name="lastName"
          label={t`Last name`}
          defaultValue={initialUser?.lastName ?? ""}
          placeholder={t`E.g. Taylor`}
          className="sm:flex-1"
        />
      </div>

      <TextField
        name="email"
        label={t`Email`}
        tooltip={t`Your email address cannot be changed. An owner must delete your account and reinvite you with the new email address.`}
        value={initialUser?.email ?? ""}
        readOnly={true}
        startIcon={<MailIcon className="size-4" />}
      />

      <TextField
        name="title"
        label={t`Title`}
        defaultValue={initialUser?.title ?? ""}
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
        <div className="flex flex-col gap-4">
          {fieldsSection}
          {infoFields}
        </div>
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
