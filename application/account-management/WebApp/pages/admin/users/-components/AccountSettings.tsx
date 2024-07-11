import { Button } from "@repo/ui/components/Button";
import { DialogTrigger, Heading, Modal } from "react-aria-components";
import { Dialog } from "@repo/ui/components/Dialog";
import { Input, Label } from "@repo/ui/components/Field";
import { TextField } from "@repo/ui/components/TextField";

type AccountSettingsProps = {
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
};

export function AccountSettings({ onOpenChange, isOpen }: AccountSettingsProps) {
  return (
    <DialogTrigger isOpen={isOpen} onOpenChange={onOpenChange}>
      <Modal>
        <Dialog>
          <form onSubmit={(e) => e.preventDefault()}>
            <Heading slot="title">Account Settings</Heading>
            <TextField autoFocus>
              <Label>Name</Label>
              <Input className="input" placeholder="Name" />
            </TextField>
            <TextField>
              <Label>Domain</Label>
              <Input className="input" placeholder="Domain" />
            </TextField>
            <TextField>
              <Label>Owner</Label>
              <Input className="input" placeholder="Owner" />
            </TextField>
            <div className="flex justify-end space-x-2" style={{ marginTop: 8 }}>
              <Button onPress={() => {}} className="btn-delete">
                Delete Account
              </Button>
              <Button onPress={() => onOpenChange(false)} className="btn-cancel">
                Cancel
              </Button>
              <Button onPress={() => {}} className="btn-save">
                Save Changes
              </Button>
            </div>
          </form>
        </Dialog>
      </Modal>
    </DialogTrigger>
  );
}

export default AccountSettings;
