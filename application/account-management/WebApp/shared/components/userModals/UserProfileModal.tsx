import { AlertDialog } from "@repo/ui/components/AlertDialog";
import { FileTrigger } from "react-aria-components";
import { Button } from "@repo/ui/components/Button";
import { Modal } from "@repo/ui/components/Modal";
import { MailIcon, XIcon } from "lucide-react";
import React from "react";
import { Input } from "@repo/ui/components/Input";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { TextField } from "@repo/ui/components/TextField";
import { FieldGroup } from "@repo/ui/components/Field";
import { Label } from "@repo/ui/components/Label";
import { Avatar } from "@repo/ui/components/Avatar";

type ProfileModalProps = {
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
};

export function UserProfileModal({ isOpen, onOpenChange }: Readonly<ProfileModalProps>) {
  const [file, setFile] = React.useState<string[] | null>(null);
  const userInfo = useUserInfo();

  if (!userInfo) return null;

  return (
    <Modal isOpen={isOpen} onOpenChange={onOpenChange} isDismissable>
      <AlertDialog actionLabel="Save changes" title="" onAction={() => onOpenChange(false)}>
        <Button onPress={() => onOpenChange(false)} className="absolute top-0 right-0 m-3" variant="ghost" size="sm">
          <XIcon className="w-4 h-4" />
        </Button>
        <div className="flex flex-col text-foreground text-xl font-semibold">
          <div className="pb-4">
            <h1>Edit profile</h1>
            <h2 className="text-muted-foreground text-sm font-normal">Manage your profile here</h2>
          </div>
          <div className="w-full flex-col flex gap-3 text-muted-foreground text-sm font-medium">
            <label>Photo</label>
            <div className="flex flex-row gap-4">
              <FileTrigger
                onSelect={(e) => {
                  if (e) {
                    const files = Array.from(e);
                    const filenames = files.map((file) => file.name);
                    setFile(filenames);
                  }
                }}
              >
                <Button variant="icon" className="rounded-full">
                  <Avatar avatarUrl={userInfo.avatarUrl} initials={userInfo.initials} isRound size="md" />
                </Button>
              </FileTrigger>
              {file}
            </div>
            <div className="flex flex-row gap-4">
              <TextField label="First name" placeholder="E.g. Olivia" value={userInfo.firstName} />
              <TextField label="Last name" placeholder="E.g. Rhye" value={userInfo.lastName} />
            </div>
            <TextField>
              <Label>Email</Label>
              <FieldGroup>
                <MailIcon className="w-4 h-4" />
                <Input value={userInfo.email} isDisabled isEmbedded />
              </FieldGroup>
            </TextField>
            <TextField label="Title" placeholder="E.g. Marketing Manager" value={userInfo.title} />
          </div>
        </div>
      </AlertDialog>
    </Modal>
  );
}

export default UserProfileModal;
