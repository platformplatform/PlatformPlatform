import { AlertDialog } from "@repo/ui/components/AlertDialog";
import { FileTrigger } from "react-aria-components";
import { Button } from "@repo/ui/components/Button";
import { Modal } from "@repo/ui/components/Modal";
import { XIcon } from "lucide-react";
import avatarUrl from "../../../images/avatar.png";
import { Input } from "@repo/ui/components/Field";
import React from "react";

type ProfileModalProps = {
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
};

export function UserProfileModal({ isOpen, onOpenChange }: ProfileModalProps) {
  const [file, setFile] = React.useState<string[] | null>(null);

  return (
    <>
      <Modal isOpen={isOpen} onOpenChange={onOpenChange} isDismissable className="w-full max-w-6xl">
        <AlertDialog variant="info" actionLabel="Save changes" title="" onAction={() => onOpenChange(false)}>
          <Button onPress={() => onOpenChange(false)} className="absolute top-0 right-0 m-3" variant="icon">
            <XIcon name="cross" />
          </Button>
          <div className="flex flex-col text-gray-900 text-xl font-semibold">
            <div className="pb-4">
              <h1>Edit profile</h1>
              <h2 className=" text-slate-600 text-sm font-normal">Manage your profile here</h2>
            </div>
            <div className="w-full flex-col flex gap-3 text-slate-700 text-sm font-medium">
              <div className="flex gap-4 flex-col">
                <label>Photo</label>
                <img src={avatarUrl} alt="Userprofile" className="aspect-square w-16" />
                <FileTrigger
                  onSelect={(e) => {
                    if (e) {
                      const files = Array.from(e);
                      const filenames = files.map((file) => file.name);
                      setFile(filenames);
                    }
                  }}
                >
                  <Button variant="secondary" className="w-fit">
                    Select a file
                  </Button>
                </FileTrigger>
                {file && file}
              </div>
              <div className="flex flex-row gap-4">
                <div className="flex flex-col gap-1">
                  <label>First name</label>
                  <Input placeholder="Olivia" />
                </div>
                <div className="flex flex-col gap-1">
                  <label>Last name</label>
                  <Input placeholder="Rhye" />
                </div>
              </div>
              <div className="flex flex-col gap-1">
                <label>Email</label>
                <Input placeholder="olivia@companyx.com" />
              </div>
              <div className="flex flex-col gap-1">
                <label>Title</label>
                <Input placeholder="E.g. Marketing Manager" />
              </div>
            </div>
          </div>
        </AlertDialog>
      </Modal>
    </>
  );
}

export default UserProfileModal;
