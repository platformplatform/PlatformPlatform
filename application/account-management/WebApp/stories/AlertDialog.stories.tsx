import type { Meta } from "@storybook/react";
import { DialogTrigger } from "react-aria-components";
import { Button } from "@/ui/components/Button";
import { Modal } from "@/ui/components/Modal";
import { AlertDialog } from "@/ui/components/AlertDialog";

const meta: Meta<typeof AlertDialog> = {
  component: AlertDialog,
  parameters: {
    layout: "centered",
  },
  tags: ["autodocs"],
};

export default meta;

export function Example(args: any) {
  return (
    <DialogTrigger>
      <Button variant="secondary">Delete…</Button>
      <Modal>
        <AlertDialog {...args} />
      </Modal>
    </DialogTrigger>
  );
}

Example.args = {
  title: "Delete folder",
  children: "Are you sure you want to delete \"Documents\"? All contents will be permanently destroyed.",
  variant: "destructive",
  actionLabel: "Delete",
};
