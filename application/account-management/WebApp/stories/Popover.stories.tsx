import type { Meta } from "@storybook/react";
import { HelpCircle } from "lucide-react";
import { DialogTrigger, Heading } from "react-aria-components";
import { Button } from "@/ui/components/Button";
import { Dialog } from "@/ui/components/Dialog";
import { Popover } from "@/ui/components/Popover";

const meta: Meta<typeof Popover> = {
  component: Popover,
  parameters: {
    layout: "centered",
  },
  tags: ["autodocs"],
  args: {
    showArrow: true,
  },
};

export default meta;

export function Example(args: any) {
  return (
    <DialogTrigger>
      <Button variant="icon" aria-label="Help"><HelpCircle className="w-4 h-4" /></Button>
      <Popover {...args} className="max-w-[250px]">
        <Dialog>
          <Heading slot="title" className="text-lg font-semibold mb-2">Help</Heading>
          <p className="text-sm">For help accessing your account, please contact support.</p>
        </Dialog>
      </Popover>
    </DialogTrigger>
  );
}
