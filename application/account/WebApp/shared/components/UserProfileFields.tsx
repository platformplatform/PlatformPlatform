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
  firstNameValue?: string;
  lastNameValue?: string;
  titleValue?: string;
  onFirstNameChange?: (value: string) => void;
  onLastNameChange?: (value: string) => void;
  onTitleChange?: (value: string) => void;
  onChange?: () => void;
}

export function UserProfileFields({
  user,
  isPending,
  onAvatarFileSelect,
  onAvatarRemove,
  autoFocus,
  layout = "stacked",
  firstNameValue,
  lastNameValue,
  titleValue,
  onFirstNameChange,
  onLastNameChange,
  onTitleChange,
  onChange
}: UserProfileFieldsProps) {
  const avatarSection = (
    <UserAvatarPicker
      avatarUrl={user?.avatarUrl}
      isPending={isPending}
      onFileSelect={onAvatarFileSelect}
      onRemove={onAvatarRemove}
    />
  );

  const isControlled = firstNameValue !== undefined;

  const handleFirstNameChange = (value: string) => {
    onFirstNameChange?.(value);
    onChange?.();
  };

  const handleLastNameChange = (value: string) => {
    onLastNameChange?.(value);
    onChange?.();
  };

  const handleTitleChange = (value: string) => {
    onTitleChange?.(value);
    onChange?.();
  };

  const fieldsSection = (
    <>
      <div className="flex flex-col gap-4 sm:flex-row">
        <TextField
          autoFocus={autoFocus}
          isRequired={true}
          name="firstName"
          label={t`First name`}
          {...(isControlled ? { value: firstNameValue } : { defaultValue: user?.firstName ?? undefined })}
          onChange={isControlled ? handleFirstNameChange : onChange}
          placeholder={t`E.g. Alex`}
          className="sm:flex-1"
        />
        <TextField
          isRequired={true}
          name="lastName"
          label={t`Last name`}
          {...(isControlled ? { value: lastNameValue } : { defaultValue: user?.lastName ?? undefined })}
          onChange={isControlled ? handleLastNameChange : onChange}
          placeholder={t`E.g. Taylor`}
          className="sm:flex-1"
        />
      </div>

      <TextField
        name="email"
        label={t`Email`}
        value={user?.email ?? ""}
        isDisabled={true}
        startIcon={<MailIcon className="size-4" />}
      />

      <TextField
        name="title"
        label={t`Title`}
        tooltip={t`Your professional title or role`}
        {...(isControlled ? { value: titleValue } : { defaultValue: user?.title ?? undefined })}
        onChange={isControlled ? handleTitleChange : onChange}
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
