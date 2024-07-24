import { AlertDialog } from "@repo/ui/components/AlertDialog";
import { FileTrigger } from "react-aria-components";
import { Button } from "@repo/ui/components/Button";
import { Modal } from "@repo/ui/components/Modal";
import { Mail, XIcon } from "lucide-react";
import avatarUrl from "../topMenu/images/avatar.png";
import React from "react";
import { Input } from "@repo/ui/components/Input";

type ProfileModalProps = {
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
};

export function UserProfileModal({ isOpen, onOpenChange }: Readonly<ProfileModalProps>) {
  const [file, setFile] = React.useState<string[] | null>(null);

  return (
    <Modal isOpen={isOpen} onOpenChange={onOpenChange} isDismissable>
      <AlertDialog actionLabel="Save changes" title="" onAction={() => onOpenChange(false)}>
        <Button onPress={() => onOpenChange(false)} className="absolute top-0 right-0 m-3" variant="ghost" size="sm">
          <XIcon className="w-4 h-4" />
        </Button>
        <div className="flex flex-col text-gray-900 text-xl font-semibold">
          <div className="pb-4">
            <h1>Edit profile</h1>
            <h2 className=" text-slate-600 text-sm font-normal">Manage your profile here</h2>
          </div>
          <div className="w-full flex-col flex gap-3 text-slate-700 text-sm font-medium">
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
                <Button variant="icon" className="rounded-full bg-transparent">
                  <img src={avatarUrl} alt="Userprofile" className="aspect-square w-16" />
                </Button>
              </FileTrigger>
              {file}
            </div>
            <div className="flex flex-row gap-4">
              <div className="flex flex-col gap-1">
                <label>First name</label>
                <Input placeholder="E.g. Olivia" />
              </div>
              <div className="flex flex-col gap-1">
                <label>Last name</label>
                <Input placeholder="E.g. Rhye" />
              </div>
            </div>
            <div className="flex flex-col gap-1">
              <label>Email</label>
              <div>
                <Input value="olivia@companyx.com" isDisabled={true} className="border px-4 py-2 pl-10 w-full" />
                <Mail className="absolute -translate-y-7 translate-x-2 text-gray-400 size-5" />
              </div>
            </div>
            <div className="flex flex-col gap-1">
              <label>Title</label>
              <Input placeholder="E.g. Marketing Manager" />
            </div>
          </div>
        </div>
      </AlertDialog>
    </Modal>
  );
}

export default UserProfileModal;
